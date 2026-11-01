using Infinity.Toolkit;
using Infinity.Toolkit.Handlers;
using Infinity.Toolkit.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Odin.Modules.AzureDevOps.PullRequests.Contracts.DomainEvents;
using Odin.Modules.AzureDevOps.PullRequests.Contracts.Models;
using Odin.Modules.AzureDevOps.PullRequests.IntegrationEvents;
using Odin.Modules.AzureDevOps.PullRequests.Models;
using System.Text.Json;

namespace Odin.Modules.AzureDevOps.PullRequests.Handlers;

internal class GitPullRequestEventHandler(ILogger<GitPullRequestEventHandler> logger) : IRequestHandler<GitPullRequestEvent, Result>
{
    public Task<Result> HandleAsync(IRequestHandlerContext<GitPullRequestEvent> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received Git Pull Request Event from Service Bus > {EventType} : {PullRequestId}", context?.Request?.EventType, context?.Request?.Resource?.PullRequestId);
        var jsonDocument = JsonDocument.Parse(context?.Body);
        var resourceElement = jsonDocument.RootElement.GetProperty("resource");
        return Task.FromResult(Result.Success());
    }
}

internal class GitPullRequestMessageHandler(IRequestHandler<GitPullRequestEvent, Result> requestHandler, IServiceProvider serviceProvider, ILogger<GitPullRequestMessageHandler> logger) : IMessageHandler<GitPullRequestEvent>
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task Handle(IMessageHandlerContext<GitPullRequestEvent> context, CancellationToken cancellationToken)
    {
        var requestContext = HandlerContextExtensions.Create<GitPullRequestEvent>(context.Message!);
        await requestHandler!.HandleAsync(requestContext, cancellationToken);
        //logger.LogInformation("Received Git Pull Request Event from Service Bus > {EventType} : {PullRequestId}", context.Message!.EventType, context.Message.Resource.PullRequestId);
        //var jsonDocument = JsonDocument.Parse(context?.Body);
        //var resourceElement = jsonDocument.RootElement.GetProperty("resource");

        //_ = context?.Message?.EventType switch
        //{
        //    "git.pullrequest.created" => HandlePullRequestCreatedAsync(resourceElement, cancellationToken),
        //    "git.pullrequest.updated" => HandlePullRequestUpdatedAsync(context.Message, resourceElement, cancellationToken),
        //    "git.pullrequest.merged" => HandlePullRequestMergedAsync(resourceElement, cancellationToken),
        //    "ms.vss-code.git-pullrequest-comment-event" => HandlePullRequestCommentAsync(resourceElement, cancellationToken),
        //    _ => Task.FromResult("Unknown Event Type")
        //};

    }

    private async Task HandlePullRequestCreatedAsync(JsonElement resourceElement, CancellationToken cancellationToken)
    {
        var resource = resourceElement.Deserialize<GitPullRequestResource>(jsonSerializerOptions);

        ArgumentNullException.ThrowIfNull(resource, "Pull Request Resource is null");

        var evt = CreateDomainEvent<PullRequestCreated>(resourceElement);
        var channelProducer = serviceProvider.GetRequiredService<IChannelProducer<PullRequestCreated>>();
        await channelProducer.SendAsync(evt, cancellationToken: cancellationToken);
    }

    private async Task HandlePullRequestUpdatedAsync(GitPullRequestEvent gitPullrequestIntegrationEvent, JsonElement resourceElement, CancellationToken cancellationToken)
    {
        var updateReason = GitPullRequestParser.ParseUpdateReason(gitPullrequestIntegrationEvent.Message.Text);
        logger?.LogInformation("Update Reason: {UpdateReason}", updateReason);

        var pullRequestUpdatedEvent = CreateDomainEvent<PullRequestUpdatedEvent>(resourceElement) with
        {
            Reason = updateReason,
            Reviewers = JsonSerializer.Deserialize<Contracts.Models.Reviewer[]>(resourceElement.GetProperty("reviewers").GetRawText(), jsonSerializerOptions) ?? [],
            ClosedDate = updateReason == PullRequestUpdateReason.Completed ? DateTimeOffset.Parse(resourceElement.GetProperty("closedDate").GetString()!) : default,
            ClosedBy = updateReason == PullRequestUpdateReason.Completed ? JsonSerializer.Deserialize<Contracts.Models.ClosedBy>(resourceElement.GetProperty("closedBy").GetRawText(), jsonSerializerOptions) : default,
        };

        var channelProducer = serviceProvider.GetRequiredService<IChannelProducer<PullRequestUpdatedEvent>>();
        await channelProducer.SendAsync(pullRequestUpdatedEvent, cancellationToken: cancellationToken);
    }

    private async Task HandlePullRequestMergedAsync(JsonElement resourceElement, CancellationToken cancellationToken)
    {
        var evt = CreateDomainEvent<PullRequestMergeCommitCreated>(resourceElement);
        var channelProducer = serviceProvider.GetRequiredService<IChannelProducer<PullRequestMergeCommitCreated>>();
        await channelProducer.SendAsync(evt, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Handles the pull request comment event
    ///
    /// Need to deserialize the message to get the comment details
    /// The message has another payload that contains the comment details
    /// {
    ///     "eventType": "ms.vss-code.git-pullrequest-comment-event",
    ///     "resource": {
    ///         "comment": {},
    ///         "pullRequest": {}
    ///     }
    /// }
    /// </summary>
    private async Task HandlePullRequestCommentAsync(JsonElement resourceElement, CancellationToken cancellationToken)
    {
        var pullRequestElement = resourceElement.GetProperty("pullRequest");
        var commentElement = resourceElement.GetProperty("comment");

        var resource = pullRequestElement.Deserialize<GitPullRequestResource>(jsonSerializerOptions);
        var commentResource = commentElement.Deserialize<GitPullRequestCommentResource>(jsonSerializerOptions);

        ArgumentNullException.ThrowIfNull(resource, "Pull Request Resource is null");
        ArgumentNullException.ThrowIfNull(commentResource, "Comment Resource is null");

        var evt = CreateDomainEvent<PullRequestComment>(pullRequestElement);
        var pullRequestComment = evt with
        {
            CommentId = commentResource.Id,
            ParentCommentId = commentResource.ParentCommentId,
            CommentType = commentResource.CommentType,
            Content = commentResource.Content,
            Author = commentResource.Author.DisplayName,
            PublishedDate = commentResource.PublishedDate,
        };

        var channelProducer = serviceProvider.GetRequiredService<IChannelProducer<PullRequestComment>>();
        await channelProducer.SendAsync(pullRequestComment, cancellationToken: cancellationToken);
    }

    private T CreateDomainEvent<T>(JsonElement resourceElement)
        where T : PullRequestEventBase, new()
    {
        var resource = resourceElement.Deserialize<GitPullRequestResource>(jsonSerializerOptions);
        ArgumentNullException.ThrowIfNull(resource, $"Could not deserialize json to {nameof(T)}");

        var evt = new T
        {
            CreatedBy = resource.CreatedBy,
            CreationDate = DateTimeOffset.Parse(resource.CreationDate),
            PullRequestId = resource.PullRequestId,
            PullRequestUrl = resource.Links.Web.Href,
            RepositoryId = resource.Repository.Id,
            RepositoryName = resource.Repository.Name,
            RepositoryUrl = resource.Repository.Url,
            ProjectId = resource.Repository.Project.Id,
            ProjectName = resource.Repository.Project.Name,
            Title = resource.Title,
            Description = resource.Description,
            Status = resource.Status,
            MergeStatus = resource.MergeStatus,
            SourceRefName = resource.SourceRefName,
            TargetRefName = resource.TargetRefName
        };

        return evt;
    }
}
