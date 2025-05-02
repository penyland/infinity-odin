using Microsoft.AspNetCore.Mvc;
using Odin.Api.Features.OpenApi;
using RabbitMQ.Client;
using System.Reflection;

namespace Odin.Api.Features.Messaging;

public class MessagingModule : WebFeatureModule
{
    public override IModuleInfo ModuleInfo { get; } = new FeatureModuleInfo(typeof(OpenApiModule).FullName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    public override void MapEndpoints(WebApplication app) => app.MapMessagingEndpoints();

    public override ModuleContext RegisterModule(ModuleContext context)
    {
        return context;
    }
}

public static class MessagingEndpoints
{
    public static RouteGroupBuilder MapMessagingEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/messaging")
            .WithTags("Messaging");

        group.MapPost("send", ([FromServices] IConnection connection) =>
        {
            var channel = connection.CreateModel();
            var queueName = "test-queue";
            channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            var message = "Hello World!";
            var body = System.Text.Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
            return Results.Ok($"Sent message: {message}");
        });

        return group;
    }
}
