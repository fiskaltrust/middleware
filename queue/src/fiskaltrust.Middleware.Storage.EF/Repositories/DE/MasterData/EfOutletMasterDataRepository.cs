using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.DE.MasterData
{
    public class EfOutletMasterDataRepository : AbstractEFRepostiory<Guid, OutletMasterData>, IMasterDataRepository<OutletMasterData>
    {
        public EfOutletMasterDataRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task ClearAsync() => await RemoveAll().ConfigureAwait(false);

        public async Task CreateAsync(OutletMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(OutletMasterData entity) { }

        protected override Guid GetIdForEntity(OutletMasterData entity) => entity.OutletId;
    }
}
