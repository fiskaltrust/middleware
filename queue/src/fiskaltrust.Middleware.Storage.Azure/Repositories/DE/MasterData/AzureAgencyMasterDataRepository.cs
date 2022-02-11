using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Storage.Azure.Mapping;
using fiskaltrust.Middleware.Storage.Azure.TableEntities;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories.DE
{
    public class AzureAgencyMasterDataRepository : BaseAzureTableRepository<Guid, AzureAgencyMasterData, AgencyMasterData>, IMasterDataRepository<AgencyMasterData>
    {
        public AzureAgencyMasterDataRepository(Guid queueId, string connectionString)
            : base(queueId, connectionString, nameof(AgencyMasterData)) { }

        public async Task ClearAsync() => await ClearTableAsync().ConfigureAwait(false);

        public async Task CreateAsync(AgencyMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(AgencyMasterData entity) { }

        protected override Guid GetIdForEntity(AgencyMasterData entity) => entity.AgencyId;

        protected override AzureAgencyMasterData MapToAzureEntity(AgencyMasterData entity) => Mapper.Map(entity);

        protected override AgencyMasterData MapToStorageEntity(AzureAgencyMasterData entity) => Mapper.Map(entity);
    }
}
