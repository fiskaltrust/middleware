using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Migrations
{
    public class Migration_007_BE : IAzureTableStorageMigration
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueConfiguration _queueConfiguration;

        public Migration_007_BE(TableServiceClient tableServiceClient, QueueConfiguration queueConfiguration)
        {
            _tableServiceClient = tableServiceClient;
            _queueConfiguration = queueConfiguration;
        }

        public int Version => 7;

        public async Task ExecuteAsync()
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueueBERepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageSignaturCreationUnitBERepository.TABLE_NAME));
        }

        private string GetTableName(string entityName) => $"x{_queueConfiguration.QueueId.ToString().Replace("-", "")}{entityName}";
    }
}