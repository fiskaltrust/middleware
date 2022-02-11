using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.DE.MasterData
{
    public class EfAccountMasterDataRepository : AbstractEFRepostiory<Guid, AccountMasterData>, IMasterDataRepository<AccountMasterData>
    {
        public EfAccountMasterDataRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        public async Task ClearAsync() => await RemoveAll().ConfigureAwait(false);

        public async Task CreateAsync(AccountMasterData entity) => await InsertAsync(entity).ConfigureAwait(false);

        protected override void EntityUpdated(AccountMasterData entity) { }

        protected override Guid GetIdForEntity(AccountMasterData entity) => entity.AccountId;
    }
}
