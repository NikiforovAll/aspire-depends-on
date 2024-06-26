# Nall.Aspire.Hosting.DependsOn

[![Build](https://github.com/NikiforovAll/aspire-depends-on/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/NikiforovAll/aspire-depends-on/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/dt/Nall.Aspire.Hosting.DependsOn.svg)](https://nuget.org/packages/Nall.Aspire.Hosting.DependsOn)
[![contributionswelcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](https://github.com/nikiforovall/aspire-depends-on)
[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-yellow.svg)](https://conventionalcommits.org)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/nikiforovall/aspire-depends-on/blob/main/LICENSE.md)

DependsOn functionality for .NET Aspire. Control startup dependencies between components.

## Install

```bash
dotnet add package Nall.Aspire.Hosting.DependsOn.All
```

| Package                                    | Version                                                                                                                                                      | Description                                                        |
| ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------ |
| `Nall.Aspire.Hosting.DependsOn`            | [![Nuget](https://img.shields.io/nuget/v/Nall.Aspire.Hosting.DependsOn.svg)](https://nuget.org/packages/Nall.Aspire.Hosting.DependsOn)                       | Dependencies Core                                                  |
| `Nall.Aspire.Hosting.DependsOn.All`        | [![Nuget](https://img.shields.io/nuget/v/Nall.Aspire.Hosting.DependsOn.All.svg)](https://nuget.org/packages/Nall.Aspire.Hosting.DependsOn.All)               | Meta Package containing common dependencies for popular components |
| `Nall.Aspire.Hosting.DependsOn.Uris`       | [![Nuget](https://img.shields.io/nuget/v/Nall.Aspire.Hosting.DependsOn.Uris.svg)](https://nuget.org/packages/Nall.Aspire.Hosting.DependsOn.Uris)             | HttpEndpoints health check                                         |
| `Nall.Aspire.Hosting.DependsOn.PostgreSQL` | [![Nuget](https://img.shields.io/nuget/v/Nall.Aspire.Hosting.DependsOn.PostgreSQL.svg)](https://nuget.org/packages/Nall.Aspire.Hosting.DependsOn.PostgreSQL) | PostgreSQL health check                                            |
| `Nall.Aspire.Hosting.DependsOn.SqlServer`  | [![Nuget](https://img.shields.io/nuget/v/Nall.Aspire.Hosting.DependsOn.SqlServer.svg)](https://nuget.org/packages/Nall.Aspire.Hosting.DependsOn.SqlServer)   | SqlServer health check                                             |
| `Nall.Aspire.Hosting.DependsOn.RabbitMQ`   | [![Nuget](https://img.shields.io/nuget/v/Nall.Aspire.Hosting.DependsOn.RabbitMQ.svg)](https://nuget.org/packages/Nall.Aspire.Hosting.DependsOn.RabbitMQ)     | RabbitMQ health check                                              |

## Examples

```csharp
// AppHost/Program.cs
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
```

![alt](/assets/demo-depends-on.gif)

## Build and Development

`dotnet cake --target build`

`dotnet cake --target test`

`dotnet cake --target pack`

## References

- <https://github.com/davidfowl/WaitForDependenciesAspire>
