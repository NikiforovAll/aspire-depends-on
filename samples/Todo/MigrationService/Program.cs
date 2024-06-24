using Data;
using Microsoft.EntityFrameworkCore;
using MigrationService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<DbInitializer>();

builder.AddServiceDefaults();

builder
    .Services.AddOpenTelemetry()
    .WithTracing(tracing =>
        tracing.AddSource(DbInitializer.ActivitySourceName)
    );

builder.Services.AddDbContextPool<TodoDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("db"),
        sqlOptions =>
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                null
            )
    )
);

var app = builder.Build();

app.Run();
