using Odin.Modules.AzureDevOps.PullRequests.Models;

namespace Odin.Modules.AzureDevOps.PullRequests.IntegrationEvents;

internal record GitPullRequestEvent : BasePayload<GitPullRequestResource>
{
}
