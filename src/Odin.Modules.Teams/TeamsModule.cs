using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Odin.Modules.AzureDevOps.PullRequests.Contracts.DomainEvents;

namespace Odin.Modules.Teams;

[WebFeatureModule("Odin.Modules.Teams", "1.0.0")]
internal class TeamsModule : WebFeatureModule
{
    public override void RegisterModule(IHostApplicationBuilder builder)
    {
        builder.Services.AddEventHandler<PullRequestCreated, PullRequestCreatedHandler>();
    }
}

internal class PullRequestCreatedHandler(ILogger<PullRequestCreatedHandler> logger) : IEventHandler<PullRequestCreated>
{
    private readonly ILogger<PullRequestCreatedHandler> logger = logger;

    public Task HandleAsync(IEventHandlerContext<PullRequestCreated> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received Pull Request Created Event > {PullRequestId}", context?.Event?.PullRequestId);
        return Task.CompletedTask;
    }
}
