using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.DE.MasterData
{
    public class EFCoreOutletMasterDataRepository : AbstractEFCoreRepostiory<Guid, OutletMasterData>, IMasterDataRepository<OutletMasterData>
    {
        public EFCoreOutletMasterDataRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task ClearAsync() => await RemoveAll();

        public async Task CreateAsync(OutletMasterData entity) => await InsertAsync(entity);

        protected override void EntityUpdated(OutletMasterData entity) { }

        protected override Guid GetIdForEntity(OutletMasterData entity) => entity.OutletId;
    }
}
