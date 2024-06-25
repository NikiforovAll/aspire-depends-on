var builder = DistributedApplication.CreateBuilder(args);

builder.Services.Configure<DependsOnOptions>(
    builder.Configuration.GetRequiredSection("DependsOnOptions")
);

var api0 = builder
    .AddProject<Projects.WebApplication2>("api0")
    .WithHealthCheck();

builder
    .AddProject<Projects.WebApplication1>("api")
    .WaitFor(api0);

builder.Build().Run();
