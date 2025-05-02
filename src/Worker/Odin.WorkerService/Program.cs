using Odin.WorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<WorkerDbContext>("Postgres");
builder.AddRabbitMQClient("Messaging");

builder.Services.AddHostedService<ProcessingJob>();

var host = builder.Build();
host.Run();
