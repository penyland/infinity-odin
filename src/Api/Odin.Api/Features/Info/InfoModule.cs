using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.AspNetCore.Http.HttpResults;
using Odin.Api.Features.OpenApi;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Odin.Api.Features.Info;

public class InfoModule : WebFeatureModule
{
    public override IModuleInfo ModuleInfo { get; } = new FeatureModuleInfo(typeof(OpenApiModule).FullName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    public override void MapEndpoints(WebApplication app) => app.MapInfoEndpoints();
}

public static class InfoEndpoints
{
    private static readonly string[] ForbiddenKeys = ["ConnectionString", "Auth", "Secret"];

    public static RouteGroupBuilder MapInfoEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/info")
            .WithTags("Info");

        group.MapGet("/version", GetVersion)
            .WithName("GetVersion")
            .WithDisplayName("Get service version")
            .Produces<Info>(StatusCodes.Status200OK);

        group.MapGet("/config", GetConfig)
            .Produces<string>(StatusCodes.Status200OK)
            .RequireAuthorization();

        group.MapGet("/modules", GetFeatureModuleInfos)
            .Produces<IEnumerable<FeatureModuleInfo>>(StatusCodes.Status200OK)
            .RequireAuthorization();

        group.MapGet("/exception", () =>
        {
            throw new Exception("This is a test error");
        })
        .Produces(StatusCodes.Status500InternalServerError);

        group.MapGet("/invalidoperation", () =>
        {
            throw new InvalidOperationException("This is a InvalidOperation error");
        });

        group.MapGet("/error", (int errorCode) =>
        {
            return TypedResults.Problem("This is a test error", statusCode: errorCode);
        });

        return group;
    }

    private static JsonHttpResult<IEnumerable<FeatureModuleInfo>> GetFeatureModuleInfos(IEnumerable<IFeatureModule> featureModules)
    {
        var modules = featureModules.Select(t => new FeatureModuleInfo(t?.ModuleInfo?.Name,t?.ModuleInfo?.Version));
        return TypedResults.Json<IEnumerable<FeatureModuleInfo>>(modules);
    }

    private static ContentHttpResult GetConfig(IConfiguration configuration)
    {
        var configInfo = (configuration as IConfigurationRoot)!.GetDebugView(context => context switch
        {
            { ConfigurationProvider: AzureKeyVaultConfigurationProvider } => "******",
            { Key: var key } => ForbiddenKeys.Any(t => key.Contains(t, StringComparison.InvariantCultureIgnoreCase)) ? "******" : context.Value!,
        });

        return TypedResults.Text(configInfo);
    }

    private static JsonHttpResult<Info> GetVersion(IWebHostEnvironment webHostEnvironment)
    {
        return TypedResults.Json(new Info
        {
            Name = Assembly.GetEntryAssembly()?.GetName().Name ?? webHostEnvironment.ApplicationName ?? "Name",
            Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0",
            DateTime = DateTimeOffset.Now.UtcDateTime,
            Environment = webHostEnvironment.EnvironmentName,
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            OSVersion = Environment.OSVersion.ToString(),
            BuildDate = File.GetLastWriteTime(Assembly.GetEntryAssembly()!.Location).ToString("yyyy-MM-dd HH:mm:ss"),
            RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString()
        });
    }

    internal record Info()
    {
        [Description("The name of the service.")]
        public string? Name { get; init; }

        [Description("The version of the service.")]
        public string? Version { get; init; }

        [Description("The date and time of the request.")]
        public DateTimeOffset DateTime { get; init; }

        [Description("The environment the service is running in.")]
        public string? Environment { get; init; }

        [Description("The name of the .NET installation on which the app is running.")]
        public string? FrameworkDescription { get; init; }

        [Description("The platform identifier and version on which the app is running.")]
        public string? OSVersion { get; init; }

        [Description("The build date of the service.")]
        public string? BuildDate { get; init; }

        [Description("The platform architecture on which the current app is running.")]
        public string? OSArchitecture { get; init; }

        [Description("An opaque string that identifies the platform for which the runtime was built (or on which an app is running).")]
        public string? RuntimeIdentifier { get; init; }
    }
}
