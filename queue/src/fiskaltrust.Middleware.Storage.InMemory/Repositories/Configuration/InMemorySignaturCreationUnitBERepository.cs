using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemorySignaturCreationUnitBERepository : AbstractInMemoryRepository<Guid, ftSignaturCreationUnitBE>
    {
        public InMemorySignaturCreationUnitBERepository() : base(new List<ftSignaturCreationUnitBE>()) { }

        public InMemorySignaturCreationUnitBERepository(IEnumerable<ftSignaturCreationUnitBE> data) : base(data) { }

        protected override void EntityUpdated(ftSignaturCreationUnitBE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitBE entity) => entity.ftSignaturCreationUnitBEId;
    }
}
