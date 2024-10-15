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
    public class Migration_000_Initial : IAzureTableStorageMigration
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueConfiguration _queueConfiguration;

        public Migration_000_Initial(TableServiceClient tableServiceClient, BlobServiceClient blobServiceClient, QueueConfiguration queueConfiguration)
        {
            _tableServiceClient = tableServiceClient;
            _blobServiceClient = blobServiceClient;
            _queueConfiguration = queueConfiguration;
        }

        public int Version => 0;

        public async Task ExecuteAsync()
        {
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageJournalATRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageJournalDERepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageJournalFRRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageJournalITRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageJournalMERepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageCashBoxRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueueRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueueATRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueueDERepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueueFRRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueueITRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueueMERepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageSignaturCreationUnitATRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageSignaturCreationUnitDERepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageSignaturCreationUnitFRRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageSignaturCreationUnitITRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageSignaturCreationUnitMERepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageFailedFinishTransactionRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageFailedStartTransactionRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageOpenTransactionRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageAccountMasterDataRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageAgencyMasterDataRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageOutletMasterDataRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStoragePosSystemMasterDataRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageActionJournalRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageQueueItemRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageReceiptJournalRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageJournalFRCopyPayloadRepository.TABLE_NAME));
            await _tableServiceClient.CreateTableIfNotExistsAsync(GetTableName(AzureTableStorageReceiptReferenceIndexRepository.TABLE_NAME));

            if (!await _blobServiceClient.GetBlobContainerClient(AzureTableStorageJournalDERepository.BLOB_CONTAINER_NAME).ExistsAsync())
            {
                await _blobServiceClient.CreateBlobContainerAsync(AzureTableStorageJournalDERepository.BLOB_CONTAINER_NAME);
            }
        }

        private string GetTableName(string entityName) => $"x{_queueConfiguration.QueueId.ToString().Replace("-", "")}{entityName}";
    }
}