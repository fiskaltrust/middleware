using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryQueueESRepository : AbstractInMemoryRepository<Guid, ftQueueES>
    {
        public InMemoryQueueESRepository() : base(new List<ftQueueES>()) { }

        public InMemoryQueueESRepository(IEnumerable<ftQueueES> data) : base(data) { }

        protected override void EntityUpdated(ftQueueES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueES entity) => entity.ftQueueESId;
    }
}
