using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemorySignaturCreationUnitDERepository : AbstractInMemoryRepository<Guid, ftSignaturCreationUnitDE>
    {
        public InMemorySignaturCreationUnitDERepository() : base(new List<ftSignaturCreationUnitDE>()) { }

        public InMemorySignaturCreationUnitDERepository(IEnumerable<ftSignaturCreationUnitDE> data) : base(data) { }

        protected override void EntityUpdated(ftSignaturCreationUnitDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitDE entity) => entity.ftSignaturCreationUnitDEId;
    }
}
