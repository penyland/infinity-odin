using Odin.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("odin-storage")
    .RunAsEmulator(r =>
    {
        r.WithLifetime(ContainerLifetime.Persistent)
        .WithContainerName("odin")
        .WithImageTag("latest")
        .WithDataBindMount("c:/temp/azurite");
    })
    .AddTables("odin-tables");

var api = builder.AddProject<Projects.Odin_Api>("odin-api")
    .WaitFor(storage)
    .WithReference(storage)
    .WithScalarCommand();

var proxy = builder.AddProject<Projects.Odin_Proxy>("odin-proxy")
    .WaitFor(api)
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
