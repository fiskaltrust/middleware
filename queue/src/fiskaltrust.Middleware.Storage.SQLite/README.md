## Migrations

The following steps are needed to create migrations for the Middleware's database layers, e.g. when new fields are added:
Before performing any of those steps, update the fiskaltrust.storage package to the version that contains the required schema changes.

1. Create a new `<migration-name>.mysql` script file in the `Migrations` folder (make sure that you use the next number, as migrations are applied in this order)
2. Make sure that the file has the correct ending, and keep in mind that MySQL is slightly different to SQLite
3. Create or update the affected repositories