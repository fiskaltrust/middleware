using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.MasterData
{
    public class EfPosSystemMasterDataRepository : AbstractEFRepostiory<Guid, PosSystemMasterData>, IMasterDataRepository<PosSystemMasterData>
    {
        public EfPosSystemMasterDataRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task ClearAsync() => await RemoveAll().ConfigureAwait(false);

        public async Task CreateAsync(PosSystemMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(PosSystemMasterData entity) { }

        protected override Guid GetIdForEntity(PosSystemMasterData entity) => entity.PosSystemId;
    }
}
