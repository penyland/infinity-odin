using System.Globalization;
using System.Net;

namespace TestApplication.Web.Services;

public static class PullRequestServiceHookFactory
{
    private const string ProjectId = "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb";
    private const string RepositoryId = "cccccccc-1111-2222-3333-dddddddddddd";
    private const string AccountId = "eeeeeeee-1111-2222-3333-ffffffffffff";
    private const string CollectionId = "11111111-2222-3333-4444-555555555555";
    private const string BaseUrl = "https://dev.azure.com/contoso/";

    public static IReadOnlyList<PullRequestTestActionDefinition> Actions { get; } =
    [
        new(PullRequestTestAction.Abandoned, "Abandoned", "Send an update event with abandoned status."),
        new(PullRequestTestAction.ReviewersChanged, "Changed reviewer list", "Send an update event with a changed reviewer list."),
        new(PullRequestTestAction.AutoCompleteSet, "Autocomplete set", "Send an update event with auto-complete enabled."),
        new(PullRequestTestAction.AutoCompleteCancelled, "Autocomplete cancelled", "Send an update event with auto-complete disabled."),
        new(PullRequestTestAction.AddComment, "Add comment", "Send a pull request comment event."),
        new(PullRequestTestAction.ReplyToComment, "Reply to comment", "Send a pull request comment event with a parent comment."),
        new(PullRequestTestAction.ResolveComment, "Resolve comment", "Send a pull request comment event with a resolved thread."),
        new(PullRequestTestAction.Approve, "Approve", "Send an update event with an approving reviewer vote."),
        new(PullRequestTestAction.ApproveWithSuggestions, "Approve with suggestions", "Send an update event with an approving vote and suggestions."),
        new(PullRequestTestAction.WaitForAuthor, "Wait for author", "Send an update event with a waiting-for-author reviewer vote."),
        new(PullRequestTestAction.Reject, "Reject", "Send an update event with a rejecting reviewer vote."),
        new(PullRequestTestAction.Complete, "Complete PR", "Send a merged event with completed status."),
        new(PullRequestTestAction.Abandon, "Abandon PR", "Send an update event with abandoned status."),
        new(PullRequestTestAction.MarkAsDraft, "Marked as draft", "Send an update event with draft status."),
        new(PullRequestTestAction.SourceBranchUpdated, "Source branch updated", "Send an update event with an updated source branch.")
    ];

    public static (FakePullRequest PullRequest, PullRequestServiceHookPayload Payload) Create(
        PullRequestTestAction action,
        FakePullRequest current)
    {
        var now = DateTimeOffset.UtcNow;
        var updated = UpdatePullRequest(action, current, now);
        var eventType = action switch
        {
            PullRequestTestAction.Complete => "git.pullrequest.merged",
            PullRequestTestAction.AddComment or PullRequestTestAction.ReplyToComment or PullRequestTestAction.ResolveComment
                => "ms.vss-code.git-pullrequest-comment-event",
            _ => "git.pullrequest.updated"
        };

        var resource = action switch
        {
            PullRequestTestAction.AddComment or PullRequestTestAction.ReplyToComment or PullRequestTestAction.ResolveComment
                => CreateCommentResource(updated, action, now),
            _ => CreatePullRequestResource(updated, now)
        };

        return (updated, CreateEnvelope(eventType, updated, resource, now, action.ToString()));
    }

    public static PullRequestServiceHookPayload CreateCreated(FakePullRequest pullRequest)
    {
        var now = DateTimeOffset.UtcNow;
        return CreateEnvelope(
            "git.pullrequest.created",
            pullRequest,
            CreatePullRequestResource(pullRequest, now),
            now,
            "Created");
    }

    private static FakePullRequest UpdatePullRequest(
        PullRequestTestAction action,
        FakePullRequest current,
        DateTimeOffset now)
    {
        var updated = action switch
        {
            PullRequestTestAction.Abandoned or PullRequestTestAction.Abandon =>
                current with { Status = "abandoned", ClosedDate = now },
            PullRequestTestAction.ReviewersChanged =>
                current with { HasAdditionalReviewer = true },
            PullRequestTestAction.AutoCompleteSet =>
                current with { AutoCompleteEnabled = true },
            PullRequestTestAction.AutoCompleteCancelled =>
                current with { AutoCompleteEnabled = false },
            PullRequestTestAction.AddComment or PullRequestTestAction.ReplyToComment =>
                current with { CommentCount = current.CommentCount + 1 },
            PullRequestTestAction.ResolveComment =>
                current with { CommentCount = Math.Max(0, current.CommentCount - 1) },
            PullRequestTestAction.Approve =>
                current with { ReviewerVote = 10 },
            PullRequestTestAction.ApproveWithSuggestions =>
                current with { ReviewerVote = 5 },
            PullRequestTestAction.WaitForAuthor =>
                current with { ReviewerVote = -5 },
            PullRequestTestAction.Reject =>
                current with { ReviewerVote = -10 },
            PullRequestTestAction.Complete =>
                current with { Status = "completed", ClosedDate = now },
            PullRequestTestAction.MarkAsDraft =>
                current with { IsDraft = true },
            PullRequestTestAction.SourceBranchUpdated =>
                current with { SourceRefName = "refs/heads/feature/updated-source" },
            _ => current
        };

        return updated with { Revision = updated.Revision + 1 };
    }

    private static Dictionary<string, object?> CreatePullRequestResource(FakePullRequest pullRequest, DateTimeOffset now)
    {
        var project = new Dictionary<string, object?>
        {
            ["id"] = ProjectId,
            ["name"] = "Odin",
            ["url"] = $"{BaseUrl}Odin/_apis/projects/{ProjectId}",
            ["state"] = "wellFormed"
        };

        var repository = new Dictionary<string, object?>
        {
            ["id"] = RepositoryId,
            ["name"] = "Odin",
            ["url"] = $"{BaseUrl}Odin/_apis/git/repositories/{RepositoryId}",
            ["project"] = project,
            ["defaultBranch"] = "refs/heads/main",
            ["remoteUrl"] = $"{BaseUrl}Odin/_git/Odin"
        };

        var author = new Dictionary<string, object?>
        {
            ["id"] = "99999999-1111-2222-3333-aaaaaaaaaaaa",
            ["displayName"] = "Test PR Author",
            ["uniqueName"] = "test.author@contoso.com",
            ["url"] = $"{BaseUrl}_apis/Identities/99999999-1111-2222-3333-aaaaaaaaaaaa",
            ["imageUrl"] = $"{BaseUrl}_apis/GraphProfile/MemberAvatars/test-author"
        };

        var reviewers = new List<Dictionary<string, object?>>
        {
            CreateReviewer("bbbbbbbb-1111-2222-3333-cccccccccccc", "Test Reviewer", "reviewer@contoso.com", pullRequest.ReviewerVote),
            CreateReviewer("dddddddd-1111-2222-3333-eeeeeeeeeeee", "Second Reviewer", "second.reviewer@contoso.com", 0)
        };

        if (pullRequest.HasAdditionalReviewer)
        {
            reviewers.Add(CreateReviewer("ffffffff-1111-2222-3333-aaaaaaaaaaaa", "New Reviewer", "new.reviewer@contoso.com", 0));
        }

        var result = new Dictionary<string, object?>
        {
            ["repository"] = repository,
            ["pullRequestId"] = pullRequest.PullRequestId,
            ["codeReviewId"] = pullRequest.PullRequestId,
            ["status"] = pullRequest.Status,
            ["createdBy"] = author,
            ["creationDate"] = pullRequest.CreationDate,
            ["closedDate"] = pullRequest.ClosedDate,
            ["title"] = pullRequest.Title,
            ["description"] = pullRequest.Description,
            ["sourceRefName"] = pullRequest.SourceRefName,
            ["targetRefName"] = pullRequest.TargetRefName,
            ["mergeStatus"] = pullRequest.Status == "completed" ? "succeeded" : "notSet",
            ["lastMergeSourceCommit"] = CreateCommit("1111111111111111111111111111111111111111"),
            ["lastMergeTargetCommit"] = CreateCommit("2222222222222222222222222222222222222222"),
            ["lastMergeCommit"] = pullRequest.Status == "completed" ? CreateCommit("3333333333333333333333333333333333333333") : null,
            ["reviewers"] = reviewers,
            ["commits"] = new[] { CreateCommit("4444444444444444444444444444444444444444") },
            ["url"] = $"{BaseUrl}Odin/_apis/git/repositories/{RepositoryId}/pullRequests/{pullRequest.PullRequestId}",
            ["_links"] = new Dictionary<string, object?>
            {
                ["web"] = new Dictionary<string, object?>
                {
                    ["href"] = $"{BaseUrl}Odin/_git/Odin/pullrequest/{pullRequest.PullRequestId}"
                }
            },
            ["isDraft"] = pullRequest.IsDraft,
            ["reviewersUpdatedDate"] = now
        };

        if (pullRequest.AutoCompleteEnabled)
        {
            result["autoCompleteSetBy"] = author;
            result["completionOptions"] = new Dictionary<string, object?>
            {
                ["deleteSourceBranch"] = true,
                ["mergeCommitMessage"] = pullRequest.Title,
                ["squashMerge"] = true
            };
        }

        return result;
    }

    private static Dictionary<string, object?> CreateCommentResource(
        FakePullRequest pullRequest,
        PullRequestTestAction action,
        DateTimeOffset now)
    {
        var rootComment = CreateComment(
            1,
            0,
            "Test Reviewer",
            "reviewer@contoso.com",
            "Please consider simplifying this section.",
            pullRequest.CreationDate);
        var comments = new List<Dictionary<string, object?>> { rootComment };
        var threadStatus = "active";

        if (action == PullRequestTestAction.ReplyToComment)
        {
            comments.Add(CreateComment(
                2,
                1,
                "Test PR Author",
                "test.author@contoso.com",
                "Thanks, I will update this before merging.",
                now));
        }
        else if (action == PullRequestTestAction.AddComment)
        {
            comments = [CreateComment(
                pullRequest.CommentCount,
                0,
                "Test Reviewer",
                "reviewer@contoso.com",
                "This looks good overall.",
                now)];
        }
        else
        {
            threadStatus = "fixed";
        }

        var thread = new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["publishedDate"] = pullRequest.CreationDate,
            ["lastUpdatedDate"] = now,
            ["status"] = threadStatus,
            ["comments"] = comments
        };

        return new Dictionary<string, object?>
        {
            ["pullRequestId"] = pullRequest.PullRequestId,
            ["repository"] = CreatePullRequestResource(pullRequest, now)["repository"],
            ["discussionThread"] = thread
        };
    }

    private static Dictionary<string, object?> CreateComment(
        int id,
        int parentCommentId,
        string displayName,
        string uniqueName,
        string content,
        DateTimeOffset publishedDate)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["parentCommentId"] = parentCommentId,
            ["author"] = new Dictionary<string, object?>
            {
                ["displayName"] = displayName,
                ["uniqueName"] = uniqueName
            },
            ["content"] = content,
            ["publishedDate"] = publishedDate,
            ["lastUpdatedDate"] = publishedDate,
            ["lastContentUpdatedDate"] = publishedDate,
            ["commentType"] = "text"
        };
    }

    private static Dictionary<string, object?> CreateReviewer(
        string id,
        string displayName,
        string uniqueName,
        int vote)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["displayName"] = displayName,
            ["uniqueName"] = uniqueName,
            ["isRequired"] = true,
            ["vote"] = vote,
            ["isContainer"] = false
        };
    }

    private static Dictionary<string, object?> CreateCommit(string commitId)
    {
        return new Dictionary<string, object?>
        {
            ["commitId"] = commitId,
            ["url"] = $"{BaseUrl}Odin/_apis/git/repositories/{RepositoryId}/commits/{commitId}"
        };
    }

    private static PullRequestServiceHookPayload CreateEnvelope(
        string eventType,
        FakePullRequest pullRequest,
        object resource,
        DateTimeOffset now,
        string actionLabel)
    {
        var text = string.Create(
            CultureInfo.InvariantCulture,
            $"Pull request {pullRequest.PullRequestId}: {actionLabel} - {pullRequest.Title}");
        var message = new ServiceHookMessage(text, WebUtility.HtmlEncode(text), text);

        return new PullRequestServiceHookPayload(
            "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
            pullRequest.Revision,
            Guid.NewGuid().ToString(),
            eventType,
            "tfs",
            message,
            message,
            resource,
            "1.0",
            new ServiceHookResourceContainers(
                new ServiceHookResourceContainer(CollectionId, BaseUrl),
                new ServiceHookResourceContainer(AccountId, BaseUrl),
                new ServiceHookResourceContainer(ProjectId, $"{BaseUrl}Odin/")),
            now);
    }
}
