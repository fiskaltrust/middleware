using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitGRRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitGR, ftSignaturCreationUnitGR>
    {
        public AzureTableStorageSignaturCreationUnitGRRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitGR";

        protected override void EntityUpdated(ftSignaturCreationUnitGR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitGR entity) => entity.ftSignaturCreationUnitGRId;

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitGR storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtSignaturCreationUnitGR MapToAzureEntity(ftSignaturCreationUnitGR src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtSignaturCreationUnitGR
            {
                PartitionKey = src.ftSignaturCreationUnitGRId.ToString(),
                RowKey = src.ftSignaturCreationUnitGRId.ToString(),
                ftSignaturCreationUnitGRId = src.ftSignaturCreationUnitGRId,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftSignaturCreationUnitGR MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitGR src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitGR
            {
                ftSignaturCreationUnitGRId = src.ftSignaturCreationUnitGRId,
                TimeStamp = src.TimeStamp
            };
        }
    }
}
