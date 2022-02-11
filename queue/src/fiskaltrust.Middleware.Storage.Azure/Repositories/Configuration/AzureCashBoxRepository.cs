using System;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities.Configuration;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.Configuration
{
    public class AzureCashBoxRepository : BaseAzureTableRepository<Guid, AzureFtCashBox, ftCashBox>
    {
        public AzureCashBoxRepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(ftCashBox)) { }

        protected override void EntityUpdated(ftCashBox entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftCashBox entity) => entity.ftCashBoxId;

        protected override AzureFtCashBox MapToAzureEntity(ftCashBox entity) => Mapper.Map(entity);

        protected override ftCashBox MapToStorageEntity(AzureFtCashBox entity) => Mapper.Map(entity);
    }
}
