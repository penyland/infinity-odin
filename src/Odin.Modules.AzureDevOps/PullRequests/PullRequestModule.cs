using Infinity.Toolkit;
using Infinity.Toolkit.FeatureModules;
using Infinity.Toolkit.Handlers;
using Infinity.Toolkit.Messaging;
using Infinity.Toolkit.Messaging.AzureServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Odin.Modules.AzureDevOps.PullRequests.Handlers;
using Odin.Modules.AzureDevOps.PullRequests.Models;

namespace Odin.Modules.AzureDevOps.PullRequests;

[WebFeatureModule("Odin.Modules.AzureDevOps.PullRequests", "1.0.0")]
internal class PullRequestModule : WebFeatureModule
{
    public override void RegisterModule(IHostApplicationBuilder builder)
    {
        //builder.Services.AddRequestHandler<GitPullRequestWebHook.Command, GitPullRequestWebHook.Response, GitPullRequestWebHook.Handler>();

        builder.AddInfinityMessaging()
            .ConfigureAzureServiceBus(static configure =>
            {
                configure.AddChannelConsumer<GitPullRequestEvent>(options =>
                {
                    options.ChannelName = "toolkit-development";
                    options.SubscriptionName = "subscription1";
                    options.RequireCloudEventsTypeProperty = false;
                });
            },
            options => options.ConnectionString = builder.Configuration.GetConnectionString("AzureServiceBus"))
            .MapMessageHandler<GitPullRequestEvent, GitPullRequestEventHandler>();

        //builder.Services.AddTransient<IRequestHandler<GitPullRequestEvent, Result>, GitPullRequestEventHandler>();
        builder.Services.AddRequestHandler<GitPullRequestEvent, Result, GitPullRequestEventHandler>();

        builder.Services.AddTransient<AzureDevopsPullRequestHandler>();
    }

    public override void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/azure-devops").WithTags("AzureDevops");

        group.MapPost("/events", HandleGitPullrequestIntegrationEventAsync);
    }

    private static async Task<IResult> HandleGitPullrequestIntegrationEventAsync([FromBody] GitPullRequestEvent integrationEvent, [FromServices] IRequestHandler<GitPullRequestEvent, Result> handler)
    {
        var context = HandlerContextExtensions.Create(integrationEvent);
        await handler.HandleAsync(context, CancellationToken.None);
        return TypedResults.Ok();
    }
}

public static class HandlerContextExtensions
{
    public static IRequestHandlerContext<T> Create<T>(T request)
        where T : class
    {
        return new RequestHandlerContext<T>
        {
            Request = request,
            Body = BinaryData.FromObjectAsJson(request)
        };
    }
}
