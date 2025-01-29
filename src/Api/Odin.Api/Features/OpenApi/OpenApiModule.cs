using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Reflection;

namespace Odin.Api.Features.OpenApi;

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

        return context;
    }

    public void MapEndpoints(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            app.MapScalarApiReference(options =>
            {
                options.WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl);

                // Set authentication defaults
                var userImpersonationScope = GetScopes(app.Configuration)?.First(x => x.Contains("user_impersonation"));
                options.Authentication = new()
                {
                    PreferredSecurityScheme = "none",
                    OAuth2 = new()
                    {
                        ClientId = app.Configuration.GetValue<string>("AzureAd:ClientId"),
                        Scopes = [userImpersonationScope ?? ""],
                    }
                };
            });
        }
    }

    private static IEnumerable<string> GetScopes(IConfiguration configuration) => configuration.GetValue<string>("AzureAd:Scopes")?.Split(" ").Select(x => $"{configuration["AzureAd:AppIdentifier"]}/{x}") ?? [];

    private sealed class OAuth2SecuritySchemeDefinitionDocumentTransformer(IConfiguration configuration) : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var azureAdSection = configuration.GetRequiredSection("AzureAd");
            var scopes = azureAdSection.GetValue<string>("Scopes")?.Split(" ").ToDictionary(x => $"{azureAdSection["AppIdentifier"]}/{x}", x => x);

            var authorityUrl = new Uri($"{azureAdSection["Instance"]}{azureAdSection["TenantId"]}", UriKind.Absolute);
            var authorizationUrl = new Uri($"{authorityUrl}/oauth2/v2.0/authorize", UriKind.Absolute);
            var tokenUrl = new Uri($"{authorityUrl}/oauth2/v2.0/token", UriKind.Absolute);

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
                        AuthorizationUrl = authorizationUrl,
                        TokenUrl = tokenUrl,
                        Scopes = scopes,
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
