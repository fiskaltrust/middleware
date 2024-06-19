using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories.MasterData
{
    public class AzureTableStoragePosSystemMasterDataRepository : BaseAzureTableStorageRepository<Guid, AzureTableStoragePosSystemMasterData, PosSystemMasterData>, IMasterDataRepository<PosSystemMasterData>
    {
        public AzureTableStoragePosSystemMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, TABLE_NAME) { }

        public const string TABLE_NAME = nameof(PosSystemMasterData);

        public async Task ClearAsync() => await ClearTableAsync().ConfigureAwait(false);

        public async Task CreateAsync(PosSystemMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(PosSystemMasterData entity) { }

        protected override Guid GetIdForEntity(PosSystemMasterData entity) => entity.PosSystemId;

        protected override AzureTableStoragePosSystemMasterData MapToAzureEntity(PosSystemMasterData entity) => Mapper.Map(entity);

        protected override PosSystemMasterData MapToStorageEntity(AzureTableStoragePosSystemMasterData entity) => Mapper.Map(entity);
    }
}
