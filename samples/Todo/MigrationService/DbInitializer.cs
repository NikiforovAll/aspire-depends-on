namespace MigrationService;

using System.Diagnostics;
using Bogus;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using OpenTelemetry.Trace;

public class DbInitializer(IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public Task StartupTask => this.startupTaskCompletion.Task;
    private readonly TaskCompletionSource<bool> startupTaskCompletion = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity(
            "Migrating database",
            ActivityKind.Client
        );

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();

            await EnsureDatabaseAsync(dbContext, stoppingToken);
            await RunMigrationAsync(dbContext, stoppingToken);
            this.startupTaskCompletion.SetResult(true);
            await SeedDatabaseAsync(dbContext);
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            this.startupTaskCompletion.SetResult(false);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task SeedDatabaseAsync(TodoDbContext dbContext)
    {
        if (!await dbContext.Todos.AnyAsync())
        {
            var todoFaker = new Faker<TodoItem>()
                .UseSeed(1001)
                .RuleFor(t => t.Title, f => f.Lorem.Sentence())
                .RuleFor(t => t.IsComplete, f => f.Random.Bool());

            var todos = todoFaker.Generate(1000);

            await dbContext.Todos.AddRangeAsync(todos);
            await dbContext.SaveChangesAsync();
        }

    }

    private static async Task EnsureDatabaseAsync(
        DbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var dbCreator = dbContext.GetService<IRelationalDatabaseCreator>();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Create the database if it does not exist.
            // Do this first so there is then a database to start a transaction against.
            if (!await dbCreator.ExistsAsync(cancellationToken))
            {
                await dbCreator.CreateAsync(cancellationToken);
            }
        });
    }

    private static async Task RunMigrationAsync(
        DbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken
            );
            await dbContext.Database.MigrateAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
