using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryQueueATRepository : AbstractInMemoryRepository<Guid, ftQueueAT>
    {
        public InMemoryQueueATRepository() : base(new List<ftQueueAT>()) { }

        public InMemoryQueueATRepository(IEnumerable<ftQueueAT> data) : base(data) { }

        protected override void EntityUpdated(ftQueueAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueAT entity) => entity.ftQueueATId;
    }
}
