using Odin.Modules.AzureDevOps.PullRequests.Contracts.Models;

namespace Odin.Modules.AzureDevOps.PullRequests.Contracts.DomainEvents;

public record class PullRequestComment : PullRequestEventBase
{
    public int CommentId { get; init; }

    public int ParentCommentId { get; init; }

    public string CommentType { get; init; }

    public string Content { get; init; }

    public string Author { get; init; }

    public string PublishedDate { get; init; }
}
