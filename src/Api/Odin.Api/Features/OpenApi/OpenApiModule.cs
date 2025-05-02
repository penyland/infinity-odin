using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Reflection;

namespace Odin.Api.Features.OpenApi;

public record OpenApiOptions
{
    public string Instance { get; init; } = string.Empty;

    public string TenantId { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string Scopes { get; init; }

    public string[]? ScopesArray => Scopes.Split(" ");

    public IDictionary<string, string> ScopesDictionary => ScopesArray?.ToDictionary(x => x, x => x) ?? [];

    public string AuthorityUrl => $"{Instance}{TenantId}";

    public string AuthorizationUrl => $"{AuthorityUrl}/oauth2/v2.0/authorize";

    public string TokenUrl => $"{AuthorityUrl}/oauth2/v2.0/token";
}

public class OpenApiModule : IWebFeatureModule
{
    public IModuleInfo ModuleInfo { get; } = new FeatureModuleInfo(typeof(OpenApiModule).FullName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    public ModuleContext RegisterModule(ModuleContext context)
    {
        context.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, serviceProvider, _) =>
            {
                document.Info.Title = context.Configuration["OpenApi:Info:Title"];
                document.Info.Version = $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";
                document.Info.Description = $"{context.Configuration["OpenApi:Info:Description"]} - Environment: {context.Environment.EnvironmentName}";
                document.Info.Extensions.Add("Environment", new OpenApiString(context.Environment.EnvironmentName));

                document.Servers = [];
                return Task.CompletedTask;
            });

            options.AddDocumentTransformer<OAuth2SecuritySchemeDefinitionDocumentTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeDefinitionDocumentTransformer>();
            options.AddDocumentTransformer<AddServersDocumentTransformer>();
            options.AddOperationTransformer<SecuritySchemeOperationTransformer>();
        });

        context.Services.Configure<ScalarOptions>(context.Configuration.GetSection("Scalar"));
        context.Services.Configure<OpenApiOptions>(context.Configuration.GetSection("AzureAd"));

        return context;
    }

    public void MapEndpoints(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            var openApiOptions = app.Services.GetService<IOptions<OpenApiOptions>>();

            app.MapOpenApi();

            app.MapScalarApiReference("/", options =>
            {
                options
                    .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl)

                    .WithPreferredScheme("oauth2")
                    .AddAuthorizationCodeFlow("oauth2", flow =>
                    {
                        flow.ClientId = openApiOptions?.Value.ClientId;
                        flow.Pkce = Pkce.Sha256;
                        flow.SelectedScopes = openApiOptions?.Value.ScopesArray;
                    });
            });
        }
    }

    private static string[] GetScopes(IConfiguration configuration) => configuration.GetValue<string>("AzureAd:Scopes")?.Split(" ") ?? [];

    private sealed class OAuth2SecuritySchemeDefinitionDocumentTransformer(IOptions<OpenApiOptions> options) : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var azureAdOptions = options.Value;

            var securityScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Name = "oauth2",
                Scheme = "oauth2",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(azureAdOptions.AuthorizationUrl),
                        TokenUrl = new Uri(azureAdOptions.TokenUrl),
                        Scopes = azureAdOptions.ScopesDictionary,
                        Extensions = new Dictionary<string, IOpenApiExtension>
                        {
                            ["x-usePkce"] = new OpenApiString("SHA-256")
                        }
                    }
                }
            };

            document.Components ??= new();
            document.Components.SecuritySchemes.Add("oauth2", securityScheme);
            return Task.CompletedTask;
        }
    }

    private sealed class BearerSecuritySchemeDefinitionDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Name = "bearer",
                Scheme = "bearer",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearer" }
            };
            document.Components ??= new();
            document.Components.SecuritySchemes.Add("bearer", securityScheme);
            return Task.CompletedTask;
        }
    }

    private sealed class AddServersDocumentTransformer(IHttpContextAccessor? accessor) : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            if (accessor?.HttpContext?.Request is not { } request)
            {
                return Task.CompletedTask;
            }

            var proto = request.Headers.TryGetValue(ForwardedHeadersDefaults.XForwardedProtoHeaderName, out var values) ? values.FirstOrDefault() : null ?? request.Scheme;
            var host = request.Headers.TryGetValue(ForwardedHeadersDefaults.XForwardedHostHeaderName, out values) ? values.FirstOrDefault() : null ?? request.Host.Value;
            var prefix = request.Headers.TryGetValue(ForwardedHeadersDefaults.XForwardedPrefixHeaderName, out values) ? values.FirstOrDefault() : null;

            document.Servers = [new() { Url = $"{proto}://{host}".TrimEnd('/') }];

            return Task.CompletedTask;
        }
    }

    private sealed class SecuritySchemeOperationTransformer(IConfiguration configuration) : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
            {
                operation.Responses["401"] = new OpenApiResponse { Description = "Unauthorized" };
                operation.Responses["403"] = new OpenApiResponse { Description = "Forbidden" };

                var oauth2Scheme = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
                };

                var bearerScheme = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference{Type = ReferenceType.SecurityScheme,Id = "bearer"}
                };

                var scopes = GetScopes(configuration);
                operation.Security ??= [];
                operation.Security.Add(new() { [oauth2Scheme] = [.. scopes] });
                operation.Security.Add(new() { [bearerScheme] = [.. scopes] });
            }

            return Task.CompletedTask;
        }
    }
}
