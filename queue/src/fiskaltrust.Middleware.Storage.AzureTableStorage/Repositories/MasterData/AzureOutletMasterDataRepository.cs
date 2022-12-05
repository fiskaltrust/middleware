using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData
{
    public class AzureOutletMasterDataRepository : BaseAzureTableRepository<Guid, AzureOutletMasterData, OutletMasterData>, IMasterDataRepository<OutletMasterData>
    {
        public AzureOutletMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(OutletMasterData)) { }

        public async Task ClearAsync() => await ClearTableAsync().ConfigureAwait(false);

        public async Task CreateAsync(OutletMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(OutletMasterData entity) { }

        protected override Guid GetIdForEntity(OutletMasterData entity) => entity.OutletId;

        protected override AzureOutletMasterData MapToAzureEntity(OutletMasterData entity) => Mapper.Map(entity);

        protected override OutletMasterData MapToStorageEntity(AzureOutletMasterData entity) => Mapper.Map(entity);
    }
}
