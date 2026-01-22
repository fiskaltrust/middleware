using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Migrations
{
    public class Migration_006_GR : IAzureTableStorageMigration
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueConfiguration _queueConfiguration;

        public Migration_006_GR(TableServiceClient tableServiceClient, QueueConfiguration queueConfiguration)
        {
            _tableServiceClient = tableServiceClient;
            _queueConfiguration = queueConfiguration;
        }

        public int Version => 6;

        public async Task ExecuteAsync()
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueueGRRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageSignaturCreationUnitGRRepository.TABLE_NAME));
        }

        private string GetTableName(string entityName) => $"x{_queueConfiguration.QueueId.ToString().Replace("-", "")}{entityName}";
    }
}