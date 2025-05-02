using Odin.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithContainerName("odin-postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgWeb()
    .AddDatabase("Database", "Odin");

var rabbitmq = builder.AddRabbitMQ("RabbitMQ", port: 5672)
    .WithContainerName("odin-rabbitmq")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin(15672)
    .WithUrlForEndpoint("management", url =>
    {
        url.DisplayText = "Admin";
        url.DisplayOrder = 1;
    });

var storage = builder.AddAzureStorage("odin-storage")
    .RunAsEmulator(r =>
    {
        r.WithLifetime(ContainerLifetime.Persistent)
        .WithContainerName("odin-storage")
        .WithImageTag("latest")
        .WithDataBindMount("c:/temp/azurite");
    })
    .AddTables("odin-tables");

var api = builder.AddProject<Projects.Odin_Api>("odin-api")
    .WithReference(rabbitmq, "Messaging").WaitFor(rabbitmq)
    .WithReference(storage).WaitFor(storage)
    .WithScalarCommand();

var worker = builder.AddProject<Projects.Odin_WorkerService>("odin-worker")
    .WithReference(postgres, "Postgres").WaitFor(postgres)
    .WithReference(rabbitmq, "Messaging").WaitFor(rabbitmq);

var proxy = builder.AddProject<Projects.Odin_Proxy>("odin-proxy")
    .WaitFor(api)
    .WithReference(api)
    .WithExternalHttpEndpoints();

var angularWeb = builder.AddNpmApp("odin-angular-web", "../Web/Odin.Web.Angular")
    .WithHttpEndpoint(env: "PORT", port: 4200)
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
