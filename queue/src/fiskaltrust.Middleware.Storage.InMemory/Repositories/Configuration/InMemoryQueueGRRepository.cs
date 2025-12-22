using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryQueueGRRepository : AbstractInMemoryRepository<Guid, ftQueueGR>
    {
        public InMemoryQueueGRRepository() : base(new List<ftQueueGR>()) { }

        public InMemoryQueueGRRepository(IEnumerable<ftQueueGR> data) : base(data) { }

        protected override void EntityUpdated(ftQueueGR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueGR entity) => entity.ftQueueGRId;
    }
}
