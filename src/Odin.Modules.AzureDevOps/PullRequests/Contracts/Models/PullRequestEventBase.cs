namespace Odin.Modules.AzureDevOps.PullRequests.Contracts.Models;

public record PullRequestEventBase
{
    public GitUser CreatedBy { get; init; }

    public DateTimeOffset CreationDate { get; init; }

    public string Description { get; init; }

    public string MergeStatus { get; init; }

    public int PullRequestId { get; init; }

    public string PullRequestUrl { get; init; }

    public string RepositoryId { get; init; }

    public string RepositoryName { get; init; }

    public string RepositoryUrl { get; init; }

    public string ProjectId { get; init; }

    public string ProjectName { get; init; }

    public string SourceRefName { get; init; }

    public string Status { get; init; }

    public string TargetRefName { get; init; }

    public string Title { get; init; }
}

public record GitUser
{
    public string Id { get; init; }

    public string DisplayName { get; init; }

    public string UniqueName { get; init; }

    public string Url { get; init; }

    public string ImageUrl { get; init; }
}

public record Reviewer
{
    public string DisplayName { get; init; }

    public string UniqueName { get; init; }

    public bool IsRequired { get; init; }

    public int Vote { get; init; }
}

public record ClosedBy
{
    public string DisplayName { get; init; }

    public string UniqueName { get; init; }

    public string Url { get; init; }

    public string ImageUrl { get; init; }
}
