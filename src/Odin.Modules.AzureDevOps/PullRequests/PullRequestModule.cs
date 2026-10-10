using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Odin.Modules.AzureDevOps.PullRequests.IntegrationEvents;

namespace Odin.Modules.AzureDevOps.PullRequests;

[WebFeatureModule("Odin.Modules.AzureDevOps.PullRequests", "1.0.0")]
internal class PullRequestModule : WebFeatureModule
{
    public override void RegisterModule(IHostApplicationBuilder builder)
    {
        builder.Services.AddRequestHandler<GitPullRequestWebHook.Command, GitPullRequestWebHook.Response, GitPullRequestWebHook.Handler>();
    }

    public override void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/azure-devops").WithTags("AzureDevops");

        group.MapPost("/events", HandleGitPullrequestIntegrationEventAsync);
    }

    private static async Task<IResult> HandleGitPullrequestIntegrationEventAsync([FromBody] GitPullRequestEvent integrationEvent, [FromServices] IRequestHandler<GitPullRequestWebHook.Command, GitPullRequestWebHook.Response> handler)
    {
        var @event = new GitPullRequestWebHook.Command(integrationEvent);
        var context = HandlerContextExtensions.Create(@event);
        await handler.HandleAsync(context, CancellationToken.None);
        return TypedResults.Ok();
    }
}

internal class WebHookMessageHandlerContext(GitPullRequestEvent integrationEvent) : HandlerContext<GitPullRequestEvent>
{
    public GitPullRequestEvent IntegrationEvent { get; } = integrationEvent;
}

internal static class GitPullRequestWebHook
{
    public sealed record Command(GitPullRequestEvent Event);

    public sealed record Response(string Message);

    internal class Handler(ILogger<Handler> logger) : IRequestHandler<Command, Response>
    {
        public Task<Response> HandleAsync(IHandlerContext<Command> context, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Received Git Pull Request > {EventType} : {PullRequestId}", context.Request.Event.EventType, context.Request.Event.Resource.PullRequestId);
            return Task.FromResult(new Response($"Processed Git Pull Request Event: {context.Request.Event.Resource.PullRequestId}"));
        }
    }
}

public static class HandlerContextExtensions
{
    public static HandlerContext<T> Create<T>(T request)
    {
        return new HandlerContext<T>
        {
            Request = request,
            Body = BinaryData.FromObjectAsJson(request)
        };
    }
}
