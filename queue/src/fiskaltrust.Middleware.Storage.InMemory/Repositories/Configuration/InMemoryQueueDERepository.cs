using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryQueueDERepository : AbstractInMemoryRepository<Guid, ftQueueDE>
    {
        public InMemoryQueueDERepository() : base(new List<ftQueueDE>()) { }

        public InMemoryQueueDERepository(IEnumerable<ftQueueDE> data) : base(data) { }

        protected override void EntityUpdated(ftQueueDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueDE entity) => entity.ftQueueDEId;
    }
}
