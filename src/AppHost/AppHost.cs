var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("AzureServiceBus")
    .RunAsEmulator(emulator =>
    {
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });

var topic1 = serviceBus.AddServiceBusTopic("topic1");
topic1.AddServiceBusSubscription("topic1-subscription1");
topic1.AddServiceBusSubscription("topic1-subscription2");

var topic2 = serviceBus.AddServiceBusTopic("topic2");
topic2.AddServiceBusSubscription("topic2-subscription1");
topic2.AddServiceBusSubscription("topic2-subscription2");

var api = builder.AddProject<Projects.Odin_Api>("Api")
    .WithReference(serviceBus, "AzureServiceBus").WaitFor(serviceBus)
    .WithReference(topic1);

builder.AddViteApp("AdaptiveCardsDemo", "../adaptivecardsdemo")
    .WithReference(api).WaitFor(api)
    .WithEnvironment("VITE_API_BASE_URL", api.GetEndpoint("http"));

builder.AddProject<Projects.TestAzureDevops>("AzureDevops")
    .WithReference(serviceBus).WaitFor(serviceBus);

builder.Build().Run();
