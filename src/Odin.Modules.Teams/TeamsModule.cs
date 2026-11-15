using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;
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
        builder.Services.AddEventHandler<PullRequestCreated, PullRequestCreatedHandler>();
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
            Body =
            [
                new TextBlock($"Hello world - PR: {context?.Data?.PullRequestId}")
                {
                    Wrap = true,
                    Weight = TextWeight.Bolder
                },
                new ToggleInput("Notify me")
                {
                    Id = "notify"
                }
            ],
            Actions =
            [
                new ExecuteAction
                {
                    Title = "Submit",
                    Data = new Union<string, SubmitActionData>(new SubmitActionData
                    {
                        NonSchemaProperties = new Dictionary<string, object?>
                        {
                            { "action", "submit_basic" }
                        }
                    }),
                    AssociatedInputs = AssociatedInputs.Auto
                }]
        };

        var cardJson = JsonSerializer.Serialize(card);
        // Save to database
        // Send to Teams channel
        await streamService.PublishAsync(new PullRequestStatusUpdate(context?.Data?.PullRequestId ?? 0, "Created", cardJson));
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
