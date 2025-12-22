using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryQueueBERepository : AbstractInMemoryRepository<Guid, ftQueueBE>
    {
        public InMemoryQueueBERepository() : base(new List<ftQueueBE>()) { }

        public InMemoryQueueBERepository(IEnumerable<ftQueueBE> data) : base(data) { }

        protected override void EntityUpdated(ftQueueBE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueBE entity) => entity.ftQueueBEId;
    }
}
