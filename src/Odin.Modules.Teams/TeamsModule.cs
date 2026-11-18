using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Cards;
using Odin.Modules.AzureDevOps.PullRequests.Contracts.DomainEvents;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

namespace Odin.Modules.Teams;

[WebFeatureModule("Odin.Modules.Teams", "1.0.0")]
internal class TeamsModule : WebFeatureModule
{
    public override void RegisterModule(IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<PullRequestStatusStreamService>();
        builder.Services.AddEventHandler<PullRequestCreated, PullRequestCreatedHandler>();
        builder.Services.AddEventHandler<PullRequestUpdated, PullRequestUpdatedHandler>();
    }

    public override void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/teams").WithTags("Teams");

        group.MapGet("/pullrequests", async (PullRequestStatusStreamService streamService, HttpContext httpContext) =>
        {
            var subscriberId = Guid.NewGuid();
            var reader = streamService.Subscribe(subscriberId);
            return Results.ServerSentEvents<PullRequestStatusUpdate>(reader.ReadAllAsync(CancellationToken.None));
        });
    }
}

internal class PullRequestCreatedHandler(PullRequestStatusStreamService streamService, ILogger<PullRequestCreatedHandler> logger) : IEventHandler<PullRequestCreated>
{
    private readonly ILogger<PullRequestCreatedHandler> logger = logger;

    public async Task HandleAsync(IEventHandlerContext<PullRequestCreated> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received Pull Request Created Event > PR {PullRequestId}: {Title}", context?.Data?.PullRequestId, context?.Data?.Title);

        // Create adaptive card
        var card = new AdaptiveCard
        {
            Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
            Body =[
                new TextBlock($"{context?.Data?.Title}")
                {
                    Wrap = true,
                    Weight = TextWeight.Bolder,
                    Size = TextSize.Large
                },
                new TextBlock($"Pull Request ID: {context?.Data?.PullRequestId}")
                {
                    Wrap = true,
                    Weight = TextWeight.Default,
                    Size = TextSize.Medium
                },
                new TextBlock($"Created By: {context?.Data?.CreatedBy.DisplayName}")
                {
                    Wrap = true,
                    Weight = TextWeight.Default,
                    Size = TextSize.Medium
                },
            ],
            Actions =[]
        };

        var cardJson = JsonSerializer.Serialize(card);
        // Save to database
        // Send to Teams channel
        await streamService.PublishAsync(new PullRequestStatusUpdate(context?.Data?.PullRequestId ?? 0, "Created", cardJson));
    }
}

internal class PullRequestUpdatedHandler(PullRequestStatusStreamService streamService, ILogger<PullRequestUpdatedHandler> logger) : IEventHandler<PullRequestUpdated>
{
    private readonly ILogger<PullRequestUpdatedHandler> logger = logger;
    public async Task HandleAsync(IEventHandlerContext<PullRequestUpdated> context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received Pull Request Updated Event > PR {PullRequestId}: {Title} - Reason: {Reason}", context?.Data?.PullRequestId, context?.Data?.Title, context?.Data?.Reason);
        // Create adaptive card
        var card = new AdaptiveCard
        {
            Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
            Body =[
                new TextBlock($"{context?.Data?.Title}")
                {
                    Wrap = true,
                    Weight = TextWeight.Bolder,
                    Size = TextSize.Large
                },
                new TextBlock($"Pull Request ID: {context?.Data?.PullRequestId}")
                {
                    Wrap = true,
                    Weight = TextWeight.Default,
                    Size = TextSize.Medium
                },
                new TextBlock($"Updated By: {context?.Data?.CreatedBy.DisplayName}")
                {
                    Wrap = true,
                    Weight = TextWeight.Default,
                    Size = TextSize.Medium
                },
                new TextBlock($"Update Reason: {context?.Data?.Reason}")
                {
                    Wrap = true,
                    Weight = TextWeight.Default,
                    Size = TextSize.Medium
                },
            ],
            Actions =[]
        };
        var cardJson = JsonSerializer.Serialize(card);
        var card2 = JsonSerializer.Deserialize<AdaptiveCard>(cardJson);
        //var card2 = AdaptiveCard.Deserialize<AdaptiveCard>(cardJson);
        // Save to database
        // Send to Teams channel
        await streamService.PublishAsync(new PullRequestStatusUpdate(context?.Data?.PullRequestId ?? 0, "Updated", cardJson));
    }
}

public record PullRequestStatusUpdate(int PullRequestId, string EventType, string Content);

public class PullRequestStatusStreamService
{
    private readonly ConcurrentDictionary<Guid, Channel<PullRequestStatusUpdate>> subscribers = new();

    public ChannelReader<PullRequestStatusUpdate> Subscribe(Guid subscriberId)
    {
        var channel = subscribers.GetOrAdd(subscriberId, _ => Channel.CreateUnbounded<PullRequestStatusUpdate>());
        return channel.Reader;
    }

    public async Task PublishAsync(PullRequestStatusUpdate update)
    {
        foreach (var subscriber in subscribers.Values)
        {
            await subscriber.Writer.WriteAsync(update);
        }
    }

    public void Unsubscribe(Guid subscriberId)
    {
        if (subscribers.TryRemove(subscriberId, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }
}
