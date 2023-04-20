## Migrations

The following steps are needed to create migrations for the Middleware's database layers, e.g. when new fields are added:
Before performing any of those steps, update the fiskaltrust.storage package to the version that contains the required schema changes.

1. If a new entity was created, add a new `DBSet<T>` to `MiddlewareDbContext`
2. Configure entity keys and indices in `PostgreSQLMiddlewareDbContext.OnModelCreating()` and `SQLServerMiddlewareDbContext.OnModelCreating()`
3. Open a Powershell and install the dotnet EF tooling (`dotnet tool install --global dotnet-ef`)
4. Create new migrations in **each of the folders (SQLServer & Postgres)** `dotnet ef migrations add <migration-name>` (you may be prompted to run `dotnet ef update` first, which will update a local test DB)
5. Create or update the affected repositories