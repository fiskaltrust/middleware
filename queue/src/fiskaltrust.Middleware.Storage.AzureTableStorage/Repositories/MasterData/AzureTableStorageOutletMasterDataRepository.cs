using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData
{
    public class AzureTableStorageOutletMasterDataRepository : BaseAzureTableStorageRepository<Guid, AzureTableStorageOutletMasterData, OutletMasterData>, IMasterDataRepository<OutletMasterData>
    {
        public AzureTableStorageOutletMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(OutletMasterData)) { }

        public async Task ClearAsync() => await ClearTableAsync().ConfigureAwait(false);

        public async Task CreateAsync(OutletMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(OutletMasterData entity) { }

        protected override Guid GetIdForEntity(OutletMasterData entity) => entity.OutletId;

        protected override AzureTableStorageOutletMasterData MapToAzureEntity(OutletMasterData entity) => Mapper.Map(entity);

        protected override OutletMasterData MapToStorageEntity(AzureTableStorageOutletMasterData entity) => Mapper.Map(entity);
    }
}
