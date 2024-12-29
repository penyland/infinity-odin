var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Odin_Api>("odin-api");

builder.Build().Run();
