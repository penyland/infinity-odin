namespace TestAzureDevops.Services;

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

public sealed record ServiceHookMessage
{
    public string Text { get; init; }

    public string Html { get; init; }

    public string Markdown { get; init; }
}


public sealed record ServiceHookResourceContainers(
    ServiceHookResourceContainer Collection,
    ServiceHookResourceContainer Account,
    ServiceHookResourceContainer Project);

public sealed record ServiceHookResourceContainer(string Id, string BaseUrl);
