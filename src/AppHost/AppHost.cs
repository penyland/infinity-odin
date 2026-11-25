using AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddAzureSqlServer("AzureSqlServer")
    .RunAsContainer(emulator =>
    {
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });

var teamsdb = sqlServer.AddDatabase("odin-teams");

var serviceBus = builder.AddAzureServiceBus("AzureServiceBus")
    .RunAsEmulator(emulator =>
    {
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });

var topic1 = serviceBus.AddServiceBusTopic("topic1");
topic1.AddServiceBusSubscription("topic1-subscription1");
topic1.AddServiceBusSubscription("topic1-subscription2");

var topic2 = serviceBus.AddServiceBusTopic("topic2")
    .AddServiceBusSubscription("topic2-subscription1");

var api = builder.AddDotnetProject("Api", "../Api/Odin.Api")
    .WithReference(serviceBus, "AzureServiceBus").WaitFor(serviceBus)
    .WithReference(teamsdb).WaitFor(teamsdb);

builder.AddTestApplications(serviceBus, api);

builder.Build().Run();
