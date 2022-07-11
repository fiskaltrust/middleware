using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData
{
    public class InMemoryOutletMasterDataRepository : AbstractInMemoryRepository<Guid, OutletMasterData>, IMasterDataRepository<OutletMasterData>
    {
        public InMemoryOutletMasterDataRepository() : base(new List<OutletMasterData>()) { }
        public InMemoryOutletMasterDataRepository(IEnumerable<OutletMasterData> data) : base(data) { }

        public Task CreateAsync(OutletMasterData entity)
        {
            Data.Add(entity.OutletId, entity);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            Data.Clear();
            return Task.CompletedTask;
        }

        protected override void EntityUpdated(OutletMasterData entity) { }

        protected override Guid GetIdForEntity(OutletMasterData entity) => entity.OutletId;
    }
}
