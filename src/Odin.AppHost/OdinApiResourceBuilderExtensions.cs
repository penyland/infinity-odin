using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Odin.AppHost;

internal static class OdinApiResourceBuilderExtensions
{
    public static IResourceBuilder<T> WithScalarCommand<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithEndpoints
    {
        builder.WithCommand(
            name: "scalar-docs",
            displayName: "Scalar api reference",
            executeCommand: context => OnOpenUrlCommandAsync(builder, context, "scalar"),
            updateState: OnUpdateStateResource,
            iconName: "AnimalRabbitOff",
            iconVariant: IconVariant.Filled);

        return builder;
    }

    private static async Task<ExecuteCommandResult> OnOpenUrlCommandAsync<T>(IResourceBuilder<T> builder, ExecuteCommandContext context, string url)
        where T : IResourceWithEndpoints
    {
        var endpoint = builder.Resource.GetEndpoint("https").Url;
        var theUrl = $"{endpoint}/{url}";
        Console.WriteLine(theUrl);
        await Task.Run(() => Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = theUrl,
        }));
        return CommandResults.Success();
    }

    private static ResourceCommandState OnUpdateStateResource(UpdateCommandStateContext context)
    {
        return context.ResourceSnapshot.HealthStatus == HealthStatus.Healthy
                    ? ResourceCommandState.Enabled
                    : ResourceCommandState.Disabled;
    }
}
