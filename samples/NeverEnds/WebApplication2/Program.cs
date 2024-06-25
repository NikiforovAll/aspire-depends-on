using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHealthChecks().AddAsyncCheck("c0", () =>
    Task.FromResult(HealthCheckResult.Unhealthy(
        "Service unable to start. This is intentional")));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapDefaultEndpoints();

app.Run();
