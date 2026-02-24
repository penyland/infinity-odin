using Microsoft.EntityFrameworkCore;

namespace Odin.Api.Features.VerifyConnections;

public static class VerifyConnectionsModule
{
    public static void AddVerifyConnectionsModule(this WebApplicationBuilder builder)
    {
        builder.AddSqlServerDbContext<TheDbContext>("Database");

        builder.AddAzureServiceBusClient("ServiceBus");
        builder.Services.AddHostedService<ServiceBusMessageProcessor>();
    }
}

public static class VerifyConnectionsEndpoints
{
    public static IEndpointRouteBuilder MapVerifyConnectionsEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/verify").WithTags("Verify Connections");

        group.MapGet("/database", async (TheDbContext context) =>
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync("SELECT 1");
                return Results.Text($"[{DateTime.UtcNow}] Database connection successful.");
            }
            catch (Exception ex)
            {
                return Results.Text($"[{DateTime.UtcNow}] Database connection failed: {ex.Message}");
            }
        });

        group.MapPost("/servicebus/send", async (ServiceBusClient serviceBusClient, IConfiguration configuration) =>
        {
            try
            {
                var topicName = configuration["ServiceBus:TopicName"] 
                    ?? throw new InvalidOperationException("ServiceBus:TopicName configuration is missing");
                
                await using var sender = serviceBusClient.CreateSender(topicName);
                
                var message = new ServiceBusMessage($"Test message sent at {DateTime.UtcNow:O}")
                {
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString()
                };
                
                await sender.SendMessageAsync(message);
                
                return Results.Ok(new 
                { 
                    Success = true, 
                    Message = "Message sent successfully", 
                    MessageId = message.MessageId,
                    Timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Failed to send message to Service Bus",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        });
        
        return builder;
    }
}

public class TheDbContext(DbContextOptions<YourDbContext> options) : DbContext(options)
{
}
