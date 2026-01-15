using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitBERepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitBE, ftSignaturCreationUnitBE>
    {
        public AzureTableStorageSignaturCreationUnitBERepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitBE";

        protected override void EntityUpdated(ftSignaturCreationUnitBE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitBE entity) => entity.ftSignaturCreationUnitBEId;

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitBE storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtSignaturCreationUnitBE MapToAzureEntity(ftSignaturCreationUnitBE src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtSignaturCreationUnitBE
            {
                PartitionKey = src.ftSignaturCreationUnitBEId.ToString(),
                RowKey = src.ftSignaturCreationUnitBEId.ToString(),
                ftSignaturCreationUnitBEId = src.ftSignaturCreationUnitBEId,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftSignaturCreationUnitBE MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitBE src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitBE
            {
                ftSignaturCreationUnitBEId = src.ftSignaturCreationUnitBEId,
                TimeStamp = src.TimeStamp
            };
        }
    }
}
