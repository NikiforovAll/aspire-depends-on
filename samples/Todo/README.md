# Todo app using Nall.Aspire.Hosting.DependsOn

## Migrations

```bash
dotnet ef migrations add "Initial_Migration" \
    --startup-project ./MigrationService \
    --project ./Data
```
