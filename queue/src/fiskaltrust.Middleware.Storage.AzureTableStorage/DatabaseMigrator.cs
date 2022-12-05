using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Migrations;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage
{
    public class DatabaseMigrator
    {
        private const string MIGRATION_TABLE_NAME = "ftDatabaseSchema";

        private readonly ILogger<IMiddlewareBootstrapper> _logger;
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueConfiguration _queueConfiguration;

        private readonly IAzureStorageMigration[] _migrations;

        public DatabaseMigrator(ILogger<IMiddlewareBootstrapper> logger, TableServiceClient tableServiceClient, QueueConfiguration queueConfiguration)
        {
            _logger = logger;
            _tableServiceClient = tableServiceClient;
            _queueConfiguration = queueConfiguration;

            _migrations = new[]
            {
                new Migration_000_Initial(_tableServiceClient, queueConfiguration)
            };
        }

        public async Task MigrateAsync()
        {
            if (await MigrationTableExists())
            {
                var currentMigration = await GetCurrentMigrationAsync();
                await ExecuteMigrationsAsync(_migrations.Where(x => x.Version > currentMigration));
            }
            else
            {
                _logger.LogInformation("Database tables were not yet created, executing all {MigrationCount} migrations.", _migrations.Length);
                await _tableServiceClient.CreateTableIfNotExistsAsync(GetMigrationTableName());
                await ExecuteMigrationsAsync(_migrations);
            }
        }

        private async Task ExecuteMigrationsAsync(IEnumerable<IAzureStorageMigration> migrations)
        {
            foreach (var migration in migrations.OrderBy(x => x.Version))
            {
                await migration.ExecuteAsync();
                await SetCurrentMigrationAsync(migration.Version);
                _logger.LogDebug("Applied migration {MigrationName} (version: {MigrationVersion}).", migration.GetType().Name, migration.Version);
            }
        }

        private async Task<int> GetCurrentMigrationAsync()
        {
            var tableClient = _tableServiceClient.GetTableClient(GetMigrationTableName());
            var migration = await tableClient.QueryAsync<TableEntity>().FirstOrDefaultAsync();
            return migration?.GetInt32("CurrentVersion") ?? -1;
        }

        private async Task SetCurrentMigrationAsync(int version)
        {
            var tableClient = _tableServiceClient.GetTableClient(GetMigrationTableName());
            await tableClient.UpsertEntityAsync(new TableEntity(_queueConfiguration.QueueId.ToString(), "Current") { { "CurentVersion", version } });
        }

        private async Task<bool> MigrationTableExists() => await _tableServiceClient.QueryAsync(t => t.Name == GetMigrationTableName()).AnyAsync();

        private string GetMigrationTableName() => $"x{_queueConfiguration.QueueId.ToString().Replace("-", "")}{MIGRATION_TABLE_NAME}";
    }
}