using Odin.Modules.AzureDevOps.PullRequests.Contracts.DomainEvents;

namespace Odin.Modules.AzureDevOps.PullRequests.Handlers;

internal static class GitPullRequestParser
{
    public static PullRequestUpdateReason ParseUpdateReason(string message)
    {
        return message switch
        {
            string when message.Contains("created pull request", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.Created,
            string when message.Contains("abandoned pull request", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.Abandoned,
            string when message.Contains("approved pull request", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.Approved,
            string when message.Contains("approved and left suggestions", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.ApprovedWithSuggestions,
            string when message.Contains("changed the reviewer list", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.ChangedReviewerList,
            string when message.Contains("completed pull request", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.Completed,
            string when message.Contains("marked the pull request as completed", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.Completed,
            string when message.Contains("updated pull request", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.DescriptionUpdated,
            string when message.Contains("marked the pull request as a draft", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.MarkedAsDraft,
            string when message.Contains("published the pull request", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.Published,
            string when message.Contains("reactivated pull request", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.Reactivated,
            string when message.Contains("rejected pull request", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.Rejected,
            string when message.Contains("voted on pull request", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.ResetFeedback,
            string when message.Contains("updated the source branch", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.SourceBranchUpdated,
            string when message.Contains("is waiting for the author", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.WaitingForAuthor,
            _ => PullRequestUpdateReason.Unknown
            //string when message.Contains("autocomplete cancelled", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.AutocompleteCancelled,
            //string when message.Contains("merged", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.Merged,
            //string when message.Contains("set autocomplete", StringComparison.OrdinalIgnoreCase) => PullRequestUpdateReason.SetAutocomplete,
        };
    }
}
