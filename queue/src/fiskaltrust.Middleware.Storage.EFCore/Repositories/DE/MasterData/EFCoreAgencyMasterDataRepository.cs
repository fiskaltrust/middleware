using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.DE.MasterData
{
    public class EFCoreAgencyMasterDataRepository : AbstractEFCoreRepostiory<Guid, AgencyMasterData>, IMasterDataRepository<AgencyMasterData>
    {
        public EFCoreAgencyMasterDataRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task ClearAsync() => await RemoveAll();

        public async Task CreateAsync(AgencyMasterData entity) => await InsertAsync(entity);

        protected override void EntityUpdated(AgencyMasterData entity) { }

        protected override Guid GetIdForEntity(AgencyMasterData entity) => entity.AgencyId;
    }
}
