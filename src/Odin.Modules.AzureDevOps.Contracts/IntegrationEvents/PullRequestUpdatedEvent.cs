using Odin.Modules.AzureDevOps.PullRequests.Contracts.Models;

namespace Odin.Modules.AzureDevOps.PullRequests.Contracts.DomainEvents;

public enum PullRequestUpdateReason
{
    Abandoned,
    Approved,
    ApprovedWithSuggestions,
    AutocompleteCancelled,
    ChangedReviewerList,
    Completed,
    Created,
    DescriptionUpdated,
    MarkedAsDraft,
    Merged,
    Published,
    Reactivated,
    Rejected,
    ResetFeedback,
    SetAutocomplete,
    SourceBranchUpdated,
    WaitingForAuthor,
    Unknown
}

public record PullRequestUpdatedEvent : PullRequestEventBase
{
    public PullRequestUpdateReason Reason { get; init; }

    public Reviewer[] Reviewers { get; init; }

    public DateTimeOffset ClosedDate { get; init; }

    public ClosedBy? ClosedBy { get; init; }
}
