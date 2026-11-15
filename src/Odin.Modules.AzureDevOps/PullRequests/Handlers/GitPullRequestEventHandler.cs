using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Infinity.Toolkit.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using Odin.Modules.AzureDevOps.PullRequests.Models;

namespace Odin.Modules.AzureDevOps.PullRequests.Handlers;

internal class GitPullRequestEventHandler(PullRequestHandler azureDevopsPullRequestHandler, ILogger<GitPullRequestEventHandler> logger) : IRequestHandler<GitPullRequestEvent, Result>, IMessageHandler<GitPullRequestEvent>
{
    public async Task Handle(IMessageHandlerContext<GitPullRequestEvent> context, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received Git Pull Request Event from Service Bus > {EventType} : {PullRequestId}", context?.Message?.EventType, context?.Message?.Resource?.PullRequestId);
        var handlerContext = new HandlerContext<GitPullRequestEvent>
        {
            Payload = context?.Payload ?? BinaryData.FromObjectAsJson(context?.Message),
            Content = context?.Message!
        };

        await azureDevopsPullRequestHandler.HandleAsync(handlerContext, cancellationToken);
    }

    public async Task<Result> HandleAsync(IRequestHandlerContext<GitPullRequestEvent> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received Git Pull Request Event from Service Bus > {EventType} : {PullRequestId}", context?.Request?.EventType, context?.Request?.Resource?.PullRequestId);

        var handlerContext = new HandlerContext<GitPullRequestEvent>
        {
            Content = context?.Request!,
            Payload = context?.Payload!
        };

        await azureDevopsPullRequestHandler.HandleAsync(handlerContext, cancellationToken);

        return Result.Success();
    }
}

public class HandlerContext<TBody> : IHandlerContext
{
    public TBody Content { get; init; }
    public required BinaryData Payload { get; init; }
}
