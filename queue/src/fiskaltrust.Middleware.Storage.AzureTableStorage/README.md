## Migrations

The following steps are needed to create migrations for the Middleware's database layers, e.g. when new fields are added:
Before performing any of those steps, update the fiskaltrust.storage package to the version that contains the required schema changes.

1. Create Azure Storage classes in the `TableEntities` namespace that inherit from `BaseTableEntity`
2. Create or update the `Mappings.cs` to contain methods to convert from/to the Azure Storage classes
3. Create a new migration in the `Migrations` namespace that inherits from `IAzureTableStorageMigration` and add it to the list of migrations in `DatabaseMigrator.cs`
4. Create or update the affected repositories