using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemorySignaturCreationUnitESRepository : AbstractInMemoryRepository<Guid, ftSignaturCreationUnitES>
    {
        public InMemorySignaturCreationUnitESRepository() : base(new List<ftSignaturCreationUnitES>()) { }

        public InMemorySignaturCreationUnitESRepository(IEnumerable<ftSignaturCreationUnitES> data) : base(data) { }

        protected override void EntityUpdated(ftSignaturCreationUnitES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitES entity) => entity.ftSignaturCreationUnitESId;
    }
}
