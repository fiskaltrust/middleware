using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryQueueITRepository : AbstractInMemoryRepository<Guid, ftQueueIT>
    {
        public InMemoryQueueITRepository() : base(new List<ftQueueIT>()) { }

        public InMemoryQueueITRepository(IEnumerable<ftQueueIT> data) : base(data) { }

        protected override void EntityUpdated(ftQueueIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueIT entity) => entity.ftQueueITId;
    }
}
