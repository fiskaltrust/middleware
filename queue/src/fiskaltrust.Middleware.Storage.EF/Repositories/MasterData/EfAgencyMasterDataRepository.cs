using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.MasterData
{
    public class EfAgencyMasterDataRepository : AbstractEFRepostiory<Guid, AgencyMasterData>, IMasterDataRepository<AgencyMasterData>
    {
        public EfAgencyMasterDataRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task ClearAsync() => await RemoveAll().ConfigureAwait(false);

        public async Task CreateAsync(AgencyMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(AgencyMasterData entity) { }

        protected override Guid GetIdForEntity(AgencyMasterData entity) => entity.AgencyId;
    }
}
