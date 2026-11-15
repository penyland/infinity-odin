using Azure.Messaging.ServiceBus;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace TestApplication.Web.Services;

public sealed class PullRequestEventPublisher(ServiceBusClient serviceBusClient) : IAsyncDisposable
{
    internal static readonly ActivitySource ActivitySource = new("testapplication-web", Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    public const string TopicName = "toolkit-development";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ServiceBusSender sender = serviceBusClient.CreateSender(TopicName);

    public async Task SendAsync(PullRequestServiceHookPayload payload, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity($"publish {TopicName}", ActivityKind.Producer)?.SetTag("messaging.system", "AzureServiceBus");
        activity?.SetTag("messaging.system", "AzureServiceBus");
        activity?.SetTag("messaging.operation.type", "publish");

        var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var message = new ServiceBusMessage(BinaryData.FromBytes(body))
        {
            ContentType = "application/json",
            MessageId = payload.Id,
            Subject = payload.EventType
        };

        // 00-<TraceId>-<SpanId>-01
        message.ApplicationProperties.Add("messagebus.traceparent", $"00-{activity!.TraceId}-{activity!.SpanId}-01");
        message.ApplicationProperties.Add("messagebus.tracestate", activity.TraceStateString);

        await sender.SendMessageAsync(message, cancellationToken);
    }

    public ValueTask DisposeAsync() => sender.DisposeAsync();
}
