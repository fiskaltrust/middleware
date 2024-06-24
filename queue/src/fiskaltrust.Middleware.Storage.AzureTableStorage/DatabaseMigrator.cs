using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Migrations;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage
{
    public class DatabaseMigrator
    {
        private const string MIGRATION_TABLE_NAME = "ftDatabaseSchema";

        private readonly ILogger<IMiddlewareBootstrapper> _logger;
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueConfiguration _queueConfiguration;

        public bool? MigratedFrom1_2 { get; set; }

        private readonly IAzureTableStorageMigration[] _migrations;

        public DatabaseMigrator(ILogger<IMiddlewareBootstrapper> logger, TableServiceClient tableServiceClient, BlobServiceClient blobServiceClient, QueueConfiguration queueConfiguration)
        {
            _logger = logger;
            _tableServiceClient = tableServiceClient;
            _queueConfiguration = queueConfiguration;
            MigratedFrom1_2 = null;

            _migrations = new IAzureTableStorageMigration[]
            {
                new Migration_000_Initial(_tableServiceClient, blobServiceClient, queueConfiguration),
                new Migration_001_TableNameFix(_tableServiceClient, queueConfiguration)
            };
        }

        public async Task MigrateAsync()
        {
            if (await MigrationTableExists())
            {
                var currentMigration = await GetCurrentMigrationAsync();
                MigratedFrom1_2 = await GetMigratedFrom1_2Async();
                await ExecuteMigrationsAsync(_migrations.Where(x => x.Version > currentMigration));
            }
            else
            {
                _logger.LogInformation("Database tables were not yet created, executing all {MigrationCount} migrations.", _migrations.Length);
                await _tableServiceClient.CreateTableIfNotExistsAsync(GetMigrationTableName());

                if (await _tableServiceClient.QueryAsync(t => t.Name == AzureTableStorageCashBoxRepository.TABLE_NAME).AnyAsync())
                {
                    var cashBoxTableClient = _tableServiceClient.GetTableClient(AzureTableStorageCashBoxRepository.TABLE_NAME);
                    var cashBoxes = await cashBoxTableClient.QueryAsync<TableEntity>().FirstOrDefaultAsync();
                    if (cashBoxes is not null)
                    {
                        MigratedFrom1_2 = true;
                    }
                }
                await ExecuteMigrationsAsync(_migrations);
            }
        }

        private async Task ExecuteMigrationsAsync(IEnumerable<IAzureTableStorageMigration> migrations)
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

        private async Task<bool> GetMigratedFrom1_2Async()
        {
            var tableClient = _tableServiceClient.GetTableClient(GetMigrationTableName());
            var migration = await tableClient.QueryAsync<TableEntity>().FirstOrDefaultAsync();
            return migration?.GetBoolean(nameof(MigratedFrom1_2)) ?? false;
        }

        private async Task SetCurrentMigrationAsync(int version)
        {
            var tableClient = _tableServiceClient.GetTableClient(GetMigrationTableName());
            await tableClient.UpsertEntityAsync(new TableEntity(_queueConfiguration.QueueId.ToString(), "Current") { { "CurrentVersion", version }, { nameof(MigratedFrom1_2), MigratedFrom1_2 } });
        }

        private async Task<bool> MigrationTableExists() => await _tableServiceClient.QueryAsync(t => t.Name == GetMigrationTableName()).AnyAsync();

        private string GetMigrationTableName() => $"x{_queueConfiguration.QueueId.ToString().Replace("-", "")}{MIGRATION_TABLE_NAME}";
    }
}