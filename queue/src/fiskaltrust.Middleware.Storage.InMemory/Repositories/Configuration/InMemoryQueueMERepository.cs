using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryQueueMERepository : AbstractInMemoryRepository<Guid, ftQueueME>
    {
        public InMemoryQueueMERepository() : base(new List<ftQueueME>()) { }

        public InMemoryQueueMERepository(IEnumerable<ftQueueME> data) : base(data) { }

        protected override void EntityUpdated(ftQueueME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueME entity) => entity.ftQueueMEId;
    }
}
