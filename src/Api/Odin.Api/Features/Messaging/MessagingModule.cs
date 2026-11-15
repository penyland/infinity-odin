using Infinity.Toolkit.Messaging;
using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.AzureServiceBus;
using Microsoft.AspNetCore.Mvc;

namespace Odin.Api.Features.Messaging;

[WebFeatureModule("Odin.Api.Features.Messaging", "1.0.0")]
public class MessagingModule : WebFeatureModule
{
    public override void MapEndpoints(WebApplication app) => app.MapMessagingEndpoints();

    public override void RegisterModule(IHostApplicationBuilder builder)
    {
        builder.AddInfinityMessaging()
            .ConfigureAzureServiceBus(b =>
            {
                b.AddChannelProducer<TestEvent>(options =>
                {
                    options.ChannelName = "toolkit-development2";
                })
                .AddChannelConsumer<TestEvent>(options =>
                {
                    options.ChannelName = "toolkit-development2";
                    options.SubscriptionName = "subscription2";
                });
            })
            .MapMessageHandler<TestEvent, TestEventHandler>();
    }
}

public static class MessagingEndpoints
{
    public static RouteGroupBuilder MapMessagingEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/messaging")
            .WithTags("Messaging");

        group.MapPost("send", async ([FromServices] IChannelProducer<TestEvent> producer) =>
        {
            var testEvent = new TestEvent(Guid.NewGuid().ToString(), "Test Event", DateTimeOffset.UtcNow);
            await producer.SendAsync(testEvent, CancellationToken.None);
            return Results.Ok($"Sent message: {testEvent}");
        });

        return group;
    }
}

public record TestEvent(string Id, string Name, DateTimeOffset Timestamp);

public class TestEventHandler(ILogger<TestEventHandler> logger) : IMessageHandler<TestEvent>
{
    public Task Handle(IMessageHandlerContext<TestEvent> context, CancellationToken cancellationToken)
    {
        logger?.LogInformation("Received event {Id} {Name} {TimeStamp}", context.Message!.Id, context.Message!.Name, context.Message!.Timestamp);
        return Task.CompletedTask;
    }
}
