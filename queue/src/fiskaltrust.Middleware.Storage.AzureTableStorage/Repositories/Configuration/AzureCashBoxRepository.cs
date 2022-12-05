using System;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.Configuration
{
    public class AzureCashBoxRepository : BaseAzureTableRepository<Guid, AzureFtCashBox, ftCashBox>
    {
        public AzureCashBoxRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(ftCashBox)) { }

        protected override void EntityUpdated(ftCashBox entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftCashBox entity) => entity.ftCashBoxId;

        protected override AzureFtCashBox MapToAzureEntity(ftCashBox entity) => Mapper.Map(entity);

        protected override ftCashBox MapToStorageEntity(AzureFtCashBox entity) => Mapper.Map(entity);
    }
}
