using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.DE.MasterData
{
    public class EFCoreAccountMasterDataRepository : AbstractEFCoreRepostiory<Guid, AccountMasterData>, IMasterDataRepository<AccountMasterData>
    {
        public EFCoreAccountMasterDataRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task ClearAsync() => await RemoveAll();

        public async Task CreateAsync(AccountMasterData entity) => await InsertAsync(entity);

        protected override void EntityUpdated(AccountMasterData entity) { }

        protected override Guid GetIdForEntity(AccountMasterData entity) => entity.AccountId;
    }
}
