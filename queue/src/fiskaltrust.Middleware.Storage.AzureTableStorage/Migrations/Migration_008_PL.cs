using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Migrations
{
    public class Migration_008_PL : IAzureTableStorageMigration
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueConfiguration _queueConfiguration;

        public Migration_008_PL(TableServiceClient tableServiceClient, QueueConfiguration queueConfiguration)
        {
            _tableServiceClient = tableServiceClient;
            _queueConfiguration = queueConfiguration;
        }

        public int Version => 8;

        public async Task ExecuteAsync()
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueuePLRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageSignaturCreationUnitPLRepository.TABLE_NAME));
        }

        private string GetTableName(string entityName) => $"x{_queueConfiguration.QueueId.ToString().Replace("-", "")}{entityName}";
    }
}
