using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemorySignaturCreationUnitITRepository : AbstractInMemoryRepository<Guid, ftSignaturCreationUnitIT>
    {
        public InMemorySignaturCreationUnitITRepository() : base(new List<ftSignaturCreationUnitIT>()) { }

        public InMemorySignaturCreationUnitITRepository(IEnumerable<ftSignaturCreationUnitIT> data) : base(data) { }

        protected override void EntityUpdated(ftSignaturCreationUnitIT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitIT entity) => entity.ftSignaturCreationUnitITId;
    }
}
