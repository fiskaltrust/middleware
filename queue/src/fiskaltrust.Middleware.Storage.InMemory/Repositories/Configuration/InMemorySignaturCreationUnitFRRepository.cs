using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemorySignaturCreationUnitFRRepository : AbstractInMemoryRepository<Guid, ftSignaturCreationUnitFR>
    {
        public InMemorySignaturCreationUnitFRRepository() : base(new List<ftSignaturCreationUnitFR>()) { }

        public InMemorySignaturCreationUnitFRRepository(IEnumerable<ftSignaturCreationUnitFR> data) : base(data) { }

        protected override void EntityUpdated(ftSignaturCreationUnitFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitFR entity) => entity.ftSignaturCreationUnitFRId;
    }
}
