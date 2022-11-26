using System;
using System.Threading.Tasks;
using Azure.Data.Tables;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.MasterData
{
    public class AzurePosSystemMasterDataRepository : BaseAzureTableRepository<Guid, AzurePosSystemMasterData, PosSystemMasterData>, IMasterDataRepository<PosSystemMasterData>
    {
        public AzurePosSystemMasterDataRepository(QueueConfiguration queueConfig, TableServiceClient tableServiceClient)
            : base(queueConfig, tableServiceClient, nameof(PosSystemMasterData)) { }

        public async Task ClearAsync() => await ClearTableAsync().ConfigureAwait(false);

        public async Task CreateAsync(PosSystemMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(PosSystemMasterData entity) { }

        protected override Guid GetIdForEntity(PosSystemMasterData entity) => entity.PosSystemId;

        protected override AzurePosSystemMasterData MapToAzureEntity(PosSystemMasterData entity) => Mapper.Map(entity);

        protected override PosSystemMasterData MapToStorageEntity(AzurePosSystemMasterData entity) => Mapper.Map(entity);
    }
}
