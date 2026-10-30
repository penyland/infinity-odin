var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("AzureServiceBus")
    .RunAsEmulator(emulator =>
    {
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });

var topic1 = serviceBus
    .AddServiceBusTopic("toolkit-development")
        .AddServiceBusSubscription("subscription1");

var topic2 = serviceBus
    .AddServiceBusTopic("toolkit-development2")
        .AddServiceBusSubscription("subscription2");

builder.AddProject<Projects.Odin_Api>("odin-api")
    .WithReference(serviceBus, "AzureServiceBus").WaitFor(serviceBus);

builder.AddProject<Projects.TestApplication_Web>("testapplication-web")
    .WithReference(serviceBus).WaitFor(serviceBus);

//var storage = builder.AddAzureStorage("odin-storage")
//    .RunAsEmulator(r =>
//    {
//        r.WithLifetime(ContainerLifetime.Persistent)
//        .WithContainerName("odin-storage")
//        .WithImageTag("latest")
//        .WithDataBindMount("c:/temp/azurite");
//    })
//    .AddTables("odin-tables");

//var api = builder.AddProject<Projects.Odin_Api>("odin-api")
//    //.WithReference(storage).WaitFor(storage)
//    //.WithReference(serviceBus).WaitFor(serviceBus)
//    .WithScalarCommand();

//var proxy = builder.AddProject<Projects.Odin_Proxy>("odin-proxy")
//    .WaitFor(api)
//    .WithReference(api)
//    .WithExternalHttpEndpoints();


builder.Build().Run();
