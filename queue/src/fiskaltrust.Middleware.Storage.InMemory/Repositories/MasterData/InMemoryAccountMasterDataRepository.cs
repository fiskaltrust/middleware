using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData
{
    public class InMemoryAccountMasterDataRepository : AbstractInMemoryRepository<Guid, AccountMasterData>, IMasterDataRepository<AccountMasterData>
    {
        public InMemoryAccountMasterDataRepository() : base(new List<AccountMasterData>()) { }
        public InMemoryAccountMasterDataRepository(IEnumerable<AccountMasterData> data) : base(data) { }

        public Task CreateAsync(AccountMasterData entity)
        {
            Data.Add(entity.AccountId, entity);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            Data.Clear();
            return Task.CompletedTask;
        }

        protected override void EntityUpdated(AccountMasterData entity) { }

        protected override Guid GetIdForEntity(AccountMasterData entity) => entity.AccountId;
    }
}
