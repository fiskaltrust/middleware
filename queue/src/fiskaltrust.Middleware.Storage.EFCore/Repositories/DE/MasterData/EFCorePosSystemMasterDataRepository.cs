using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.DE.MasterData
{
    public class EFCorePosSystemMasterDataRepository : AbstractEFCoreRepostiory<Guid, PosSystemMasterData>, IMasterDataRepository<PosSystemMasterData>
    {
        public EFCorePosSystemMasterDataRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task ClearAsync() => await RemoveAll();

        public async Task CreateAsync(PosSystemMasterData entity) => await InsertAsync(entity);

        protected override void EntityUpdated(PosSystemMasterData entity) { }

        protected override Guid GetIdForEntity(PosSystemMasterData entity) => entity.PosSystemId;
    }
}
