namespace TestApplication.Web.Services;

public sealed record FakePullRequest(
    int PullRequestId,
    int Revision,
    string Title,
    string Description,
    string Status,
    string SourceRefName,
    string TargetRefName,
    bool IsDraft,
    bool AutoCompleteEnabled,
    int ReviewerVote,
    bool HasAdditionalReviewer,
    DateTimeOffset CreationDate,
    DateTimeOffset? ClosedDate,
    int CommentCount);

public enum PullRequestTestAction
{
    Abandoned,
    ReviewersChanged,
    AutoCompleteSet,
    AutoCompleteCancelled,
    AddComment,
    ReplyToComment,
    ResolveComment,
    Approve,
    ApproveWithSuggestions,
    WaitForAuthor,
    Reject,
    Complete,
    Abandon,
    MarkAsDraft,
    SourceBranchUpdated
}

public sealed record PullRequestTestActionDefinition(
    PullRequestTestAction Action,
    string Label,
    string Description);
