using Azure.Messaging.ServiceBus;

namespace Odin.Api.Features.VerifyConnections;

public class ServiceBusMessageProcessor : BackgroundService, IAsyncDisposable
{
    private readonly ILogger<ServiceBusMessageProcessor> logger;
    private readonly IConfiguration configuration;
    private ServiceBusProcessor? processor;
    private readonly ServiceBusClient serviceBusClient;

    public ServiceBusMessageProcessor(ServiceBusClient serviceBusClient,
        ILogger<ServiceBusMessageProcessor> logger,
        IConfiguration configuration)
    {
        this.serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        this.logger = logger;
        this.configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var topicName = configuration["ServiceBus:TopicName"]
                ?? throw new InvalidOperationException("ServiceBus:TopicName configuration is missing");
            var subscriptionName = configuration["ServiceBus:SubscriptionName"]
                ?? throw new InvalidOperationException("ServiceBus:SubscriptionName configuration is missing");

            processor = serviceBusClient.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });

            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;

            logger.LogInformation("Starting Service Bus message processor for topic '{TopicName}' and subscription '{SubscriptionName}'",
                topicName, subscriptionName);

            await processor.StartProcessingAsync(stoppingToken);

            logger.LogInformation("Service Bus message processor started successfully");

            // Keep the processor running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Service Bus message processor is stopping");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting Service Bus message processor");
            throw;
        }
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            var body = args.Message.Body.ToString();
            logger.LogInformation("Received message: {MessageBody} (MessageId: {MessageId}, EnqueuedTime: {EnqueuedTime})",
                body,
                args.Message.MessageId,
                args.Message.EnqueuedTime);

            // Complete the message to remove it from the subscription
            await args.CompleteMessageAsync(args.Message);

            logger.LogInformation("Message processed successfully: {MessageId}", args.Message.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message: {MessageId}", args.Message.MessageId);
            // Let the message go back to the queue for retry
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception,
            "Error in Service Bus processor. Source: {ErrorSource}, Entity Path: {EntityPath}",
            args.ErrorSource,
            args.EntityPath);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Stopping Service Bus message processor");

        if (processor != null)
        {
            await processor.StopProcessingAsync(stoppingToken);
            await processor.DisposeAsync();
        }

        if (serviceBusClient != null)
        {
            await serviceBusClient.DisposeAsync();
        }

        await base.StopAsync(stoppingToken);

        logger.LogInformation("Service Bus message processor stopped");
    }

    public async ValueTask DisposeAsync()
    {
        logger.LogInformation("Disposing Service Bus message processor");

        if (processor != null)
        {
            await processor.DisposeAsync();
        }

        if (serviceBusClient != null)
        {
            await serviceBusClient.DisposeAsync();
        }

        logger.LogInformation("Service Bus message processor disposed");
    }
}
