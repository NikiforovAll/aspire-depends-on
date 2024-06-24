var builder = DistributedApplication.CreateBuilder(args);
builder.Services.Configure<DependsOnOptions>(
    builder.Configuration.GetRequiredSection("DependsOnOptions")
);

var db = builder.AddSqlServer("sql").WithHealthCheck().AddDatabase("db");

var rabbit = builder.AddRabbitMQ("rabbit").WithManagementPlugin().WithHealthCheck();

var console = builder.AddProject<Projects.ConsoleApp>("console");

var api0 = builder
    .AddProject<Projects.WebApplication2>("api-unhealthy-for-a-little-bit")
    .WithHealthCheck();

builder
    .AddProject<Projects.WebApplication1>("api")
    .WithExternalHttpEndpoints()
    .WithReference(db)
    .WithReference(rabbit)
    .WaitFor(db)
    .WaitFor(rabbit)
    .WaitFor(api0)
    .WaitForCompletion(console);

builder.Build().Run();
