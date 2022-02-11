using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryQueueRepository : AbstractInMemoryRepository<Guid, ftQueue>
    {
        public InMemoryQueueRepository() : base(new List<ftQueue>()) { }

        public InMemoryQueueRepository(IEnumerable<ftQueue> data) : base(data) { }

        protected override void EntityUpdated(ftQueue entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueue entity) => entity.ftQueueId;
    }
}
