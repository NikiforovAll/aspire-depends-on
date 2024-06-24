using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

var admin = builder.AddParameter("postgres-admin", secret: true);
var password = builder.AddParameter("postgres-password", secret: true);

builder.Services.Configure<DependsOnOptions>(builder.Configuration.GetSection("DependsOnOptions"));

var dbServer = builder.AddPostgres("db-server", admin, password, 5432).WithHealthCheck();

dbServer.WithPgAdmin(c => c.WithHostPort(5050).WaitFor(dbServer));

var db = dbServer.AddDatabase("db");

var migrator = builder
    .AddProject<Projects.MigrationService>("migrator")
    .WithReference(db)
    .WaitFor(db);

var api = builder
    .AddProject<Projects.Api>("api")
    .WithReference(db)
    .WaitForCompletion(migrator);

builder.Build().Run();
