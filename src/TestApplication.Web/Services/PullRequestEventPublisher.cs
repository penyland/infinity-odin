using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace TestApplication.Web.Services;

public sealed class PullRequestEventPublisher(ServiceBusClient serviceBusClient) : IAsyncDisposable
{
    public const string TopicName = "toolkit-development";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ServiceBusSender sender = serviceBusClient.CreateSender(TopicName);

    public async Task SendAsync(PullRequestServiceHookPayload payload, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var message = new ServiceBusMessage(BinaryData.FromBytes(body))
        {
            ContentType = "application/json",
            MessageId = payload.Id,
            Subject = payload.EventType
        };

        await sender.SendMessageAsync(message, cancellationToken);
    }

    public ValueTask DisposeAsync() => sender.DisposeAsync();
}
