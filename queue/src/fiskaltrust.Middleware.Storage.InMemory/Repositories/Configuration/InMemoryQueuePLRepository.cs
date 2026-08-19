using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryQueuePLRepository : AbstractInMemoryRepository<Guid, ftQueuePL>
    {
        public InMemoryQueuePLRepository() : base(new List<ftQueuePL>()) { }

        public InMemoryQueuePLRepository(IEnumerable<ftQueuePL> data) : base(data) { }

        protected override void EntityUpdated(ftQueuePL entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueuePL entity) => entity.ftQueuePLId;
    }
}
