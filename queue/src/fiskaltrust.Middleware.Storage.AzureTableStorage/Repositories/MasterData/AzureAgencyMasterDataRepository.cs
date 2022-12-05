using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData
{
    public class AzureAgencyMasterDataRepository : BaseAzureTableRepository<Guid, AzureAgencyMasterData, AgencyMasterData>, IMasterDataRepository<AgencyMasterData>
    {
        public AzureAgencyMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(AgencyMasterData)) { }

        public async Task ClearAsync() => await ClearTableAsync().ConfigureAwait(false);

        public async Task CreateAsync(AgencyMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(AgencyMasterData entity) { }

        protected override Guid GetIdForEntity(AgencyMasterData entity) => entity.AgencyId;

        protected override AzureAgencyMasterData MapToAzureEntity(AgencyMasterData entity) => Mapper.Map(entity);

        protected override AgencyMasterData MapToStorageEntity(AzureAgencyMasterData entity) => Mapper.Map(entity);
    }
}
