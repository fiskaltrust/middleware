## Migrations

The following steps are needed to create migrations for the Middleware's database layers, e.g. when new fields are added:
Before performing any of those steps, update the fiskaltrust.storage package to the version that contains the required schema changes.

1. If a new entity was created, add a new `DBSet<T>` to `MiddlewareDbContext`
2. Configure entity keys and indices in `MiddlewareDbContext.OnModelCreating()`
3. Open a the package manager console in VS (_Tools -> NuGet package manager -> Package Manager Console_)
4. Create a new migration with `Add-Migration <migration-name>` (you may be prompted to run `Update-Database` first, which will update a local test DB)