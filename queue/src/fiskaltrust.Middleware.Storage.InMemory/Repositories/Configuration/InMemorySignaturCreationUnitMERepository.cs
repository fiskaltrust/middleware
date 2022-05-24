using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemorySignaturCreationUnitMERepository : AbstractInMemoryRepository<Guid, ftSignaturCreationUnitME>
    {
        public InMemorySignaturCreationUnitMERepository() : base(new List<ftSignaturCreationUnitME>()) { }

        public InMemorySignaturCreationUnitMERepository(IEnumerable<ftSignaturCreationUnitME> data) : base(data) { }

        protected override void EntityUpdated(ftSignaturCreationUnitME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitME entity) => entity.ftSignaturCreationUnitMEId;
    }
}
