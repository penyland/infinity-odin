using Odin.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

//var serviceBus = builder.AddAzureServiceBus("odin-servicebus")
//    .RunAsEmulator(r =>
//    {
//        r.WithLifetime(ContainerLifetime.Persistent)
//        .WithContainerName("odin-servicebus")
//        .WithImageTag("latest");
//    })
//    .AddServiceBusTopic("odin-topic");

//var storage = builder.AddAzureStorage("odin-storage")
//    .RunAsEmulator(r =>
//    {
//        r.WithLifetime(ContainerLifetime.Persistent)
//        .WithContainerName("odin-storage")
//        .WithImageTag("latest")
//        .WithDataBindMount("c:/temp/azurite");
//    })
//    .AddTables("odin-tables");

var api = builder.AddProject<Projects.Odin_Api>("odin-api")
    //.WithReference(storage).WaitFor(storage)
    //.WithReference(serviceBus).WaitFor(serviceBus)
    .WithScalarCommand();

var proxy = builder.AddProject<Projects.Odin_Proxy>("odin-proxy")
    .WaitFor(api)
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
