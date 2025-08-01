using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.AT;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.DE;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.FR;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.IT;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.ME;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Migrations
{
    public class Migration_003_QueueEU : IAzureTableStorageMigration
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueConfiguration _queueConfiguration;

        public Migration_003_QueueEU(TableServiceClient tableServiceClient, QueueConfiguration queueConfiguration)
        {
            _tableServiceClient = tableServiceClient;
            _queueConfiguration = queueConfiguration;
        }

        public int Version => 3;

        public async Task ExecuteAsync()
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueueEURepository.TABLE_NAME));
        }

        private string GetTableName(string entityName) => $"x{_queueConfiguration.QueueId.ToString().Replace("-", "")}{entityName}";
    }
}