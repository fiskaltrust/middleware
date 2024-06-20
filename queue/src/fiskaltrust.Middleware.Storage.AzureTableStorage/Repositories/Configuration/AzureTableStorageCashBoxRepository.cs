using System;
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

        protected override AzureTableStorageFtCashBox MapToAzureEntity(ftCashBox entity) => Mapper.Map(entity);

        protected override ftCashBox MapToStorageEntity(AzureTableStorageFtCashBox entity) => Mapper.Map(entity);
    }
}
