using AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("AzureServiceBus")
    .RunAsEmulator(emulator =>
    {
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });

var topic1 = serviceBus.AddServiceBusTopic("topic1");
topic1.AddServiceBusSubscription("topic1-subscription1");
topic1.AddServiceBusSubscription("topic1-subscription2");

var api = builder.AddDotnetProject("Api", "../Api/Odin.Api")
    .WithReference(serviceBus, "AzureServiceBus").WaitFor(serviceBus);

builder.AddTestApplications(serviceBus, api);

builder.Build().Run();
