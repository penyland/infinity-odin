using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
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

                document.Servers = [];
                return Task.CompletedTask;
            });

            options.AddDocumentTransformer<OAuth2SecuritySchemeDefinitionTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeDefinitionTransformer>();
            options.AddDocumentTransformer<AddServersTransformer>();

            options.AddOperationTransformer((operation, transformerContext, ct) =>
            {
                if (transformerContext.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
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

                    var scopes = context.Configuration.GetRequiredSection("AzureAd:Scopes").GetChildren().ToDictionary(x => $"{context.Configuration["AzureAd:AppIdentifier"]}/{x}", x => x.Value);
                    operation.Security ??= [];
                    operation.Security.Add(new() { [oauth2Scheme] = [.. scopes.Keys] });
                    operation.Security.Add(new() { [bearerScheme] = [.. scopes.Keys] });
                }

                return Task.CompletedTask;
            });
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

                // Authentication defaults
                options.Authentication = new()
                {
                    OAuth2 = new()
                    {
                        ClientId = app.Configuration.GetValue<string>("AzureAd:ClientId"),
                        Scopes = [$"{app.Configuration.GetValue<string>("AzureAd:AppIdentifier")}/{app.Configuration.GetValue<string>("Scalar:Authentication:OAuth2:Scopes")}"]
                    },
                    PreferredSecurityScheme = "bearer"
                };
            });
        }
    }

    private class OAuth2SecuritySchemeDefinitionTransformer(IConfiguration configuration) : IOpenApiDocumentTransformer
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
                        Scopes = scopes
                    }
                }
            };

            document.Components ??= new();
            document.Components.SecuritySchemes.Add("oauth2", securityScheme);
            return Task.CompletedTask;
        }
    }

    private class BearerSecuritySchemeDefinitionTransformer : IOpenApiDocumentTransformer
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

    private sealed class AddServersTransformer : IOpenApiDocumentTransformer
    {
        private readonly IHttpContextAccessor? accessor;

        public AddServersTransformer(IHttpContextAccessor? accessor)
        {
            this.accessor = accessor;
        }

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
}
