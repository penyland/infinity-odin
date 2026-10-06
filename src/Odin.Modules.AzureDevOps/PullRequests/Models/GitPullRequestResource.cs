using System.Text.Json.Serialization;

namespace Odin.Modules.AzureDevOps.PullRequests.Models;

internal record GitPullRequestResource
{
    public GitRepository Repository { get; init; }

    public int PullRequestId { get; init; }

    public string Status { get; init; }

    public GitUser CreatedBy { get; init; }

    public string CreationDate { get; init; }

    public string ClosedDate { get; init; }

    public string Title { get; init; }

    public string Description { get; init; }

    public string SourceRefName { get; init; }

    public string TargetRefName { get; init; }

    public string MergeStatus { get; init; }

    public string MergeId { get; init; }

    public GitCommit LastMergeSourceCommit { get; init; }

    public GitCommit LastMergeTargetCommit { get; init; }

    public GitCommit LastMergeCommit { get; init; }

    public Reviewer[] Reviewers { get; init; }

    public GitCommit[]? Commits { get; init; }

    public string Url { get; init; }

    [JsonPropertyName("_links")]
    public Links Links { get; init; }

    public ClosedBy? ClosedBy { get; init; }
}

internal record GitRepository
{
    public string Id { get; init; }

    public string Name { get; init; }

    public string Url { get; init; }

    public Project Project { get; init; }

    public string DefaultBranch { get; init; }

    public string RemoteUrl { get; init; }
}

internal record Project
{
    public string Id { get; init; }

    public string Name { get; init; }

    public string Url { get; init; }

    public string State { get; init; }
}

internal record Reviewer
{
    public string ReviewerUrl { get; init; }

    public int Vote { get; init; }

    public string Id { get; init; }

    public bool IsRequired { get; init; }

    public string DisplayName { get; init; }

    public string UniqueName { get; init; }

    public string Url { get; init; }

    public string ImageUrl { get; init; }

    public bool IsContainer { get; init; }

    public static implicit operator Contracts.Models.Reviewer(Reviewer reviewer) => new()
    {
        DisplayName = reviewer.DisplayName,
        UniqueName = reviewer.UniqueName,
        IsRequired = reviewer.IsRequired,
        Vote = reviewer.Vote
    };
}

internal record GitCommit
{
    public string CommitId { get; init; }

    public string Url { get; init; }
}

internal record GitPullRequestCommentResource
{
    public int Id { get; init; }

    public int ParentCommentId { get; init; }

    public GitUser Author { get; init; }

    public string Content { get; init; }

    public string PublishedDate { get; init; }

    public string LastUpdatedDate { get; init; }

    public string LastContentUpdatedDate { get; init; }

    public string CommentType { get; init; }
}

internal record Links
{
    public Web Web { get; init; }
}

internal record Web
{
    public string Href { get; init; }
}

internal record ClosedBy
{
    public string DisplayName { get; init; }

    public string UniqueName { get; init; }

    public string Url { get; init; }

    public string ImageUrl { get; init; }
}
