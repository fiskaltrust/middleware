using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryQueueFRRepository : AbstractInMemoryRepository<Guid, ftQueueFR>
    {
        public InMemoryQueueFRRepository() : base(new List<ftQueueFR>()) { }

        public InMemoryQueueFRRepository(IEnumerable<ftQueueFR> data) : base(data) { }

        protected override void EntityUpdated(ftQueueFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueFR entity) => entity.ftQueueFRId;
    }
}
