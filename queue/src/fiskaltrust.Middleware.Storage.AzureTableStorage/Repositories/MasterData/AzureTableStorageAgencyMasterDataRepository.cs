using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData
{
    public class AzureTableStorageAgencyMasterDataRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageAgencyMasterData, AgencyMasterData>, IMasterDataRepository<AgencyMasterData>
    {
        public AzureTableStorageAgencyMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(AgencyMasterData)) { }

        public async Task ClearAsync() => await ClearTableAsync().ConfigureAwait(false);

        public async Task CreateAsync(AgencyMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(AgencyMasterData entity) { }

        protected override Guid GetIdForEntity(AgencyMasterData entity) => entity.AgencyId;

        protected override AzureTableStorageAgencyMasterData MapToAzureEntity(AgencyMasterData entity) => Mapper.Map(entity);

        protected override AgencyMasterData MapToStorageEntity(AzureTableStorageAgencyMasterData entity) => Mapper.Map(entity);
    }
}
