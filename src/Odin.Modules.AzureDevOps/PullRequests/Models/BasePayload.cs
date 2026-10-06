namespace Odin.Modules.AzureDevOps.PullRequests.Models;

internal record BasePayload
{
    public string Id { get; init; }

    public string EventType { get; init; }

    public string PublisherId { get; init; }

    public PayloadMessage Message { get; init; }

    public PayloadMessage DetailedMessage { get; init; }

    public string ResourceVersion { get; init; }

    public ResourceContainers ResourceContainers { get; init; }

    public string CreatedDate { get; init; }
}

/// <summary>
/// https://learn.microsoft.com/en-us/azure/devops/service-hooks/events?view=azure-devops
/// </summary>
/// <typeparam name="T"></typeparam>
internal record BasePayload<T> : BasePayload
{
    public T Resource { get; init; }
}

internal record PayloadMessage
{
    public string Text { get; init; }

    public string Html { get; init; }

    public string Markdown { get; init; }
}

internal record ResourceContainers
{
    public ResourceContainer Collection { get; init; }

    public ResourceContainer Account { get; init; }

    public ResourceContainer Project { get; init; }
}

internal record ResourceContainer(string Id);
