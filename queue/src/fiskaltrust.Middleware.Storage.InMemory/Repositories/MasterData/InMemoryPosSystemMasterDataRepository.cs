using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData
{
    public class InMemoryPosSystemMasterDataRepository : AbstractInMemoryRepository<Guid, PosSystemMasterData>, IMasterDataRepository<PosSystemMasterData>
    {
        public InMemoryPosSystemMasterDataRepository() : base(new List<PosSystemMasterData>()) { }
        public InMemoryPosSystemMasterDataRepository(IEnumerable<PosSystemMasterData> data) : base(data) { }

        public Task CreateAsync(PosSystemMasterData entity)
        {
            Data.TryAdd(entity.PosSystemId, entity);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            Data.Clear();
            return Task.CompletedTask;
        }

        protected override void EntityUpdated(PosSystemMasterData entity) { }

        protected override Guid GetIdForEntity(PosSystemMasterData entity) => entity.PosSystemId;
    }
}
