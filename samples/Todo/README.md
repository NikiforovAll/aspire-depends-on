# Todo app using Nall.Aspire.Hosting.DependsOn

Startup dependencies:

```mermaid
graph TD;
    db-server --> pgAdmin;
    db-server --> db;
    db --> migrator;
    db --> api;
    migrator --> api;
```

## Run

```bash
dotnet run --project AppHost
```

### Migrations

```bash
dotnet ef migrations add "Initial_Migration" \
    --startup-project ./MigrationService \
    --project ./Data
```
