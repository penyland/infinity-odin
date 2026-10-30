namespace TestApplication.Web.Services;

public sealed record PullRequestServiceHookPayload(
    string SubscriptionId,
    int NotificationId,
    string Id,
    string EventType,
    string PublisherId,
    ServiceHookMessage Message,
    ServiceHookMessage DetailedMessage,
    object Resource,
    string ResourceVersion,
    ServiceHookResourceContainers ResourceContainers,
    DateTimeOffset CreatedDate);

public sealed record ServiceHookMessage(string Text, string Html, string Markdown);

public sealed record ServiceHookResourceContainers(
    ServiceHookResourceContainer Collection,
    ServiceHookResourceContainer Account,
    ServiceHookResourceContainer Project);

public sealed record ServiceHookResourceContainer(string Id, string BaseUrl);
