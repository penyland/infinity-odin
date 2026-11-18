using Aspire.Hosting.Azure;
using Aspire.Hosting.Dotnet;

namespace AppHost;

internal static class Extensions
{
    public static IDistributedApplicationBuilder AddTestApplications(this IDistributedApplicationBuilder builder,
        IResourceBuilder<AzureServiceBusResource> serviceBus,
        IResourceBuilder<DotnetProjectResource> api)
    {
        builder.AddViteApp("AdaptiveCardsDemo", "../adaptivecardsdemo")
            .WithReference(api).WaitFor(api)
            .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"));

        builder.AddDotnetProject("AzureDevops", "../TestAzureDevops")
            .WithReference(serviceBus).WaitFor(serviceBus);

        return builder;
    }
}
