using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureTableStorageCashBoxRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageFtCashBox, ftCashBox>
    {
        public AzureTableStorageCashBoxRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = "CashBox";
        protected override void EntityUpdated(ftCashBox entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftCashBox entity) => entity.ftCashBoxId;

        public async Task InsertOrUpdateAsync(ftCashBox storageEntity)
        {
            EntityUpdated(storageEntity);
            var entity = MapToAzureEntity(storageEntity);
            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        protected override AzureTableStorageFtCashBox MapToAzureEntity(ftCashBox src)
        {
            if (src == null)
            {
                return null;
            }

            return new AzureTableStorageFtCashBox
            {
                PartitionKey = src.ftCashBoxId.ToString(),
                RowKey = src.ftCashBoxId.ToString(),
                ftCashBoxId = src.ftCashBoxId,
                TimeStamp = src.TimeStamp
            };
        }

        protected override ftCashBox MapToStorageEntity(AzureTableStorageFtCashBox src)
        {
            if (src == null)
            {
                return null;
            }

            return new ftCashBox
            {
                ftCashBoxId = src.ftCashBoxId,
                TimeStamp = src.TimeStamp
            };
        }
    }
}
