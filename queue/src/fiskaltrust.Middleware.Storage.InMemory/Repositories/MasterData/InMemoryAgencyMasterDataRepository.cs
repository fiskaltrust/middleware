using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.MasterData
{
    public class InMemoryAgencyMasterDataRepository : AbstractInMemoryRepository<Guid, AgencyMasterData>, IMasterDataRepository<AgencyMasterData>
    {
        public InMemoryAgencyMasterDataRepository() : base(new List<AgencyMasterData>()) { }
        public InMemoryAgencyMasterDataRepository(IEnumerable<AgencyMasterData> data) : base(data) { }

        public Task CreateAsync(AgencyMasterData entity)
        {
            Data.Add(entity.AgencyId, entity);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            Data.Clear();
            return Task.CompletedTask;
        }

        protected override void EntityUpdated(AgencyMasterData entity) { }

        protected override Guid GetIdForEntity(AgencyMasterData entity) => entity.AgencyId;
    }
}
