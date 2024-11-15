IDistributedApplicationBuilder? builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Blazor_AI_Web>("webfrontend")
    .WithExternalHttpEndpoints();

builder.Build().Run();
