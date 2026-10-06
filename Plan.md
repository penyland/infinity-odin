Implementation brief
Build a small event-driven integration service that receives Azure DevOps pull-request and build events, maintains the current state of each PR, and creates or updates one corresponding Microsoft Teams message.

The recommended stack is:

text
Azure DevOps Service Hooks
        ↓
ASP.NET Core webhook API
        ↓
Durable event queue
        ↓
Background event processor
        ↓
Azure DevOps REST API
        ↓
Teams bot + Adaptive Cards
Functional requirements
The service must:

Create one Teams message when a pull request is created.

Update that message when:

The PR receives or loses approval.

Reviewers are added or removed.

The PR status changes.

New commits are pushed.

A PR build starts or completes.

A build succeeds, partially succeeds, fails, or is canceled.

The PR is completed or abandoned.

Link to the PR, repository, build, and relevant Azure DevOps pages.

Avoid duplicate messages and duplicate processing.

Recover from temporary Azure DevOps or Teams failures.

Reconcile state by querying Azure DevOps rather than trusting individual event payloads as the complete source of truth.

Azure DevOps events
Create service-hook subscriptions for:

text
git.pullrequest.created
git.pullrequest.updated
build.complete
Optionally add pull-request comment and merge-attempt events later.

The PR-updated event should cover reviewer changes, vote changes, source-branch updates, and PR status changes. Build-completed events should update the build portion of the Teams card.

Treat service-hook events as triggers to refresh state. Events may be duplicated or arrive out of order, so fetch the current PR, reviewer, and build state from Azure DevOps before rendering the message.

API endpoint
Expose an endpoint such as:

text
POST /integrations/azure-devops/events
The endpoint should:

Authenticate and validate the incoming service-hook request.

Validate the event type and subscription.

Persist the raw event in an inbox table.

Enqueue a processing job.

Return 202 Accepted quickly.

Do not perform all Azure DevOps and Teams calls synchronously inside the webhook request.

Suggested ASP.NET Core components:

text
AzureDevOpsWebhookController
AzureDevOpsEventValidator
AzureDevOpsEventNormalizer
EventInboxService
PullRequestEventProcessor
AzureDevOpsStateClient
TeamsNotificationService
AdaptiveCardRenderer
Event processing
The background processor should follow this sequence:

Read an event from the queue.

Check whether the event has already been processed.

Extract the Azure DevOps organization, project, repository, and PR ID.

Fetch the current PR from Azure DevOps.

Fetch current reviewers and votes.

Resolve the latest relevant build status.

Load the Teams message mapping for the PR.

Render the complete Adaptive Card.

Compare the rendered card hash with the previous hash.

Create or update the Teams message only if the card changed.

Store the new state, message identifier, hash, and processing status.

Use the following logical PR key:

text
{organization}/{project}/{repositoryId}/{pullRequestId}
Persistence model
Create a PR notification mapping table similar to:

text
PullRequestNotification
-----------------------
Id
AzureDevOpsOrganization
AzureDevOpsProject
RepositoryId
PullRequestId
TeamsTenantId
TeamsTeamId
TeamsChannelId
TeamsConversationId
TeamsMessageId
LastProcessedRevision
LastProcessedEventId
LastRenderedHash
CreatedAt
UpdatedAt
Create a separate event inbox table:

text
AzureDevOpsEventInbox
---------------------
Id
ExternalEventId
EventType
Organization
Project
RepositoryId
PullRequestId
Payload
ReceivedAt
ProcessedAt
ProcessingStatus
RetryCount
LastError
Add a unique constraint for the Azure DevOps PR identity so multiple creation events cannot produce multiple Teams messages.

Add a unique constraint for the external event identifier when Azure DevOps supplies one.

Teams integration
Use a Teams bot with Adaptive Cards.

The bot should:

Be installed in the target team or channel.

Post the initial PR message.

Return and persist the Teams conversation/message/activity identifier.

Update the existing message when the PR state changes.

Support proactive messages from the background processor.

Do not use a simple incoming webhook for the primary implementation. A webhook is suitable for one-way notifications but is not the best fit for reliably maintaining one mutable message per PR.

Microsoft Graph is a valid alternative if the service already uses Graph-based identity and permissions. In that case, persist the channel message ID and use channel-message create and update APIs. The bot approach is preferable if future interactive actions or Teams-specific behavior are expected.

Adaptive Card
Render the card from current stored Azure DevOps state. Include:

text
PR title and number
Repository
Author
Source branch
Target branch
PR status
Build status
Reviewer approval summary
Pending reviewers
Last updated timestamp
Open PR button
Open build button
Example conceptual card:

text
PR #123 — Add customer export

billing-api
feature/customer-export → main

Status: Active
Build: Failed
Reviews: 2 approved, 1 pending

[Open pull request] [Open failed build]
The renderer should produce deterministic JSON so the service can calculate a hash and avoid unnecessary Teams updates.

Reliability requirements
Implement:

Idempotent event processing.

Exponential retry with maximum retry count.

Dead-letter handling for permanently failing events.

Azure DevOps API throttling handling.

Teams/Bot Framework or Graph throttling handling.

Structured logging with correlation IDs.

Metrics for received, processed, retried, failed, created, and updated events.

A reconciliation job that periodically scans active PR mappings and refreshes their state.

The reconciliation job is important because webhook delivery can fail or events can be missed.

Security
Use:

HTTPS for the webhook endpoint.

Secret or signature validation for Azure DevOps service hooks.

Managed identity or OAuth where possible.

Least-privilege Azure DevOps access.

Least-privilege Microsoft Graph permissions if Graph is used.

Secret storage in a secret manager rather than configuration files.

Validation of organization, project, repository, and event subscription.

Protection against replayed events where practical.

Do not allow arbitrary event payloads to select arbitrary Teams channels. Resolve destinations from trusted configuration.

Configuration
Support repository or project routing, for example:

json
{
  "AzureDevOps": {
    "AllowedOrganizations": ["example"],
    "WebhookPath": "/integrations/azure-devops/events"
  },
  "TeamsRouting": [
    {
      "Project": "Billing",
      "RepositoryId": "repository-id",
      "TeamId": "team-id",
      "ChannelId": "channel-id"
    }
  ]
}
Keep routing explicit and configurable. Do not derive a Teams destination directly from untrusted webhook data.

Suggested delivery phases
Phase 1: Event ingestion
Implement:

text
Azure DevOps service hooks → webhook API → event inbox → logs
Verify the actual events generated for PR creation, reviewer approval, new commits, build completion, and PR completion.

Phase 2: Initial Teams message
Add the Teams bot and create one Adaptive Card for each new PR. Persist the returned Teams message identifier.

Phase 3: Message updates
Process PR updates and build completions. Refresh current Azure DevOps state and update the existing card.

Phase 4: Reliability
Add retries, dead-letter handling, idempotency, reconciliation, metrics, and operational dashboards.

Phase 5: Interactions
Optionally add Teams buttons for opening the PR, viewing failed builds, or performing authorized Azure DevOps actions. Treat approval and completion actions as privileged operations.

Acceptance criteria
The implementation is complete when:

Creating a PR creates exactly one Teams message.

Repeated delivery of the same event does not create duplicates.

A reviewer approval updates the existing message.

A failed or completed build updates the existing message.

New commits update the build and review state.

Completing or abandoning the PR updates the existing message.

Temporary Teams or Azure DevOps failures are retried.

Missed events can be corrected by reconciliation.

The service can identify and repair mappings where the Teams message no longer exists.

The service never treats the Teams card as the source of truth; Azure DevOps remains authoritative.

The core design decision is therefore:

Build an ASP.NET Core webhook receiver and asynchronous processor, use Azure DevOps service hooks as event triggers, query Azure DevOps for authoritative current state, and use a Teams bot with Adaptive Cards to create and update one message per pull request.

