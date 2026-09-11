using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageSignaturCreationUnitPLRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtSignaturCreationUnitPL, ftSignaturCreationUnitPL>
    {
        public AzureTableStorageSignaturCreationUnitPLRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "SignaturCreationUnitPL";

        protected override void EntityUpdated(ftSignaturCreationUnitPL entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitPL entity) => entity.ftSignaturCreationUnitPLId;

        public async Task InsertOrUpdateAsync(ftSignaturCreationUnitPL storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtSignaturCreationUnitPL MapToAzureEntity(ftSignaturCreationUnitPL src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtSignaturCreationUnitPL
            {
                PartitionKey = src.ftSignaturCreationUnitPLId.ToString(),
                RowKey = src.ftSignaturCreationUnitPLId.ToString(),
                ftSignaturCreationUnitPLId = src.ftSignaturCreationUnitPLId,
                Url = src.Url,
                InfoJson = src.InfoJson,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftSignaturCreationUnitPL MapToStorageEntity(AzureTableStorageFtSignaturCreationUnitPL src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftSignaturCreationUnitPL
            {
                ftSignaturCreationUnitPLId = src.ftSignaturCreationUnitPLId,
                Url = src.Url,
                InfoJson = src.InfoJson,
                TimeStamp = src.TimeStamp
            };
        }
    }
}
