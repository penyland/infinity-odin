using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Odin.WorkerService;

internal class ProcessingJob : BackgroundService
{
    private readonly IConnection connection;
    private readonly ILogger<ProcessingJob> logger;
    private EventingBasicConsumer consumer;

    public ProcessingJob(IConnection connection, ILogger<ProcessingJob> logger)
    {
        this.connection = connection;
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = connection.CreateModel();
        channel.QueueDeclare(queue: "test-queue",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        consumer = new EventingBasicConsumer(channel);
        consumer.Received += ProcessMessageAsync;

        channel.BasicConsume(queue: "test-queue",
            autoAck: true,
            consumer: consumer);

        return Task.CompletedTask;
    }

    private void ProcessMessageAsync(object? sender, BasicDeliverEventArgs e)
    {
        var body = e.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        logger.LogInformation($"Received {message}");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        consumer.Received -= ProcessMessageAsync;
        consumer?.Model?.Close();
        connection.Close();
    }
}
