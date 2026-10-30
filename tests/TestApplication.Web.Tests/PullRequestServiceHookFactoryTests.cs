using System.Text.Json;
using TestApplication.Web.Services;

namespace TestApplication.Web.Tests;

public class PullRequestServiceHookFactoryTests
{
    [Test]
    public async Task CreateCreated_ReturnsAzureDevOpsCreatedEnvelope()
    {
        var pullRequest = CreatePullRequest();

        var payload = PullRequestServiceHookFactory.CreateCreated(pullRequest);
        var serialized = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await Assert.That(payload.EventType).IsEqualTo("git.pullrequest.created");
        await Assert.That(payload.PublisherId).IsEqualTo("tfs");
        await Assert.That(payload.ResourceVersion).IsEqualTo("1.0");
        await Assert.That(serialized).Contains("\"eventType\":\"git.pullrequest.created\"");
        await Assert.That(serialized).Contains("\"pullRequestId\":42");
    }

    [Test]
    public async Task Actions_ContainsEveryRequestedFollowUpEvent()
    {
        var labels = PullRequestServiceHookFactory.Actions.Select(action => action.Label).ToArray();

        await Assert.That(labels).Contains("Abandoned");
        await Assert.That(labels).Contains("Changed reviewer list");
        await Assert.That(labels).Contains("Autocomplete set");
        await Assert.That(labels).Contains("Autocomplete cancelled");
        await Assert.That(labels).Contains("Add comment");
        await Assert.That(labels).Contains("Reply to comment");
        await Assert.That(labels).Contains("Resolve comment");
        await Assert.That(labels).Contains("Approve");
        await Assert.That(labels).Contains("Approve with suggestions");
        await Assert.That(labels).Contains("Wait for author");
        await Assert.That(labels).Contains("Reject");
        await Assert.That(labels).Contains("Complete PR");
        await Assert.That(labels).Contains("Abandon PR");
        await Assert.That(labels).Contains("Marked as draft");
        await Assert.That(labels).Contains("Source branch updated");
    }

    [Test]
    public async Task Create_ReviewerVoteActionsSetAzureDevOpsVoteValues()
    {
        var cases = new[]
        {
            (PullRequestTestAction.Approve, 10),
            (PullRequestTestAction.ApproveWithSuggestions, 5),
            (PullRequestTestAction.WaitForAuthor, -5),
            (PullRequestTestAction.Reject, -10)
        };

        foreach (var (action, expectedVote) in cases)
        {
            var (updated, payload) = PullRequestServiceHookFactory.Create(action, CreatePullRequest());

            await Assert.That(updated.ReviewerVote).IsEqualTo(expectedVote);
            await Assert.That(payload.EventType).IsEqualTo("git.pullrequest.updated");
        }
    }

    [Test]
    public async Task Create_ReviewerAndDraftActionsUpdateResource()
    {
        var (reviewed, reviewerPayload) = PullRequestServiceHookFactory.Create(
            PullRequestTestAction.ReviewersChanged,
            CreatePullRequest());
        var reviewerResource = (Dictionary<string, object?>)reviewerPayload.Resource;
        var reviewers = (List<Dictionary<string, object?>>)reviewerResource["reviewers"]!;

        var (autoCompleted, autoCompletePayload) = PullRequestServiceHookFactory.Create(
            PullRequestTestAction.AutoCompleteSet,
            CreatePullRequest());
        var autoCompleteResource = (Dictionary<string, object?>)autoCompletePayload.Resource;

        var (draft, draftPayload) = PullRequestServiceHookFactory.Create(
            PullRequestTestAction.MarkAsDraft,
            CreatePullRequest());
        var draftResource = (Dictionary<string, object?>)draftPayload.Resource;

        var (branchUpdated, _) = PullRequestServiceHookFactory.Create(
            PullRequestTestAction.SourceBranchUpdated,
            CreatePullRequest());

        await Assert.That(reviewed.HasAdditionalReviewer).IsTrue();
        await Assert.That(reviewers.Count).IsEqualTo(3);
        await Assert.That(autoCompleted.AutoCompleteEnabled).IsTrue();
        await Assert.That(autoCompleteResource.ContainsKey("completionOptions")).IsTrue();
        await Assert.That(draft.IsDraft).IsTrue();
        await Assert.That(draftResource["isDraft"] is true).IsTrue();
        await Assert.That(branchUpdated.SourceRefName).IsEqualTo("refs/heads/feature/updated-source");
    }

    [Test]
    public async Task Create_CommentActionsUseCommentEventAndDiscussionThread()
    {
        var actions = new[]
        {
            PullRequestTestAction.AddComment,
            PullRequestTestAction.ReplyToComment,
            PullRequestTestAction.ResolveComment
        };

        foreach (var action in actions)
        {
            var (_, payload) = PullRequestServiceHookFactory.Create(action, CreatePullRequest());
            var resource = payload.Resource as Dictionary<string, object?>;

            await Assert.That(payload.EventType).IsEqualTo("ms.vss-code.git-pullrequest-comment-event");
            await Assert.That(resource).IsNotNull();
            await Assert.That(resource!.ContainsKey("discussionThread")).IsTrue();
        }
    }

    [Test]
    public async Task Create_CompletionAndAbandonActionsSetPullRequestStatus()
    {
        var (completed, completedPayload) = PullRequestServiceHookFactory.Create(
            PullRequestTestAction.Complete,
            CreatePullRequest());
        var (abandoned, abandonedPayload) = PullRequestServiceHookFactory.Create(
            PullRequestTestAction.Abandon,
            CreatePullRequest());

        await Assert.That(completed.Status).IsEqualTo("completed");
        await Assert.That(completedPayload.EventType).IsEqualTo("git.pullrequest.merged");
        await Assert.That(abandoned.Status).IsEqualTo("abandoned");
        await Assert.That(abandonedPayload.EventType).IsEqualTo("git.pullrequest.updated");
    }

    private static FakePullRequest CreatePullRequest() => new(
        42,
        1,
        "Test pull request",
        "A test description.",
        "active",
        "refs/heads/feature/test-change",
        "refs/heads/main",
        false,
        false,
        0,
        false,
        DateTimeOffset.UtcNow,
        null,
        1);
}
