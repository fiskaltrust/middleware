using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemorySignaturCreationUnitGRRepository : AbstractInMemoryRepository<Guid, ftSignaturCreationUnitGR>
    {
        public InMemorySignaturCreationUnitGRRepository() : base(new List<ftSignaturCreationUnitGR>()) { }

        public InMemorySignaturCreationUnitGRRepository(IEnumerable<ftSignaturCreationUnitGR> data) : base(data) { }

        protected override void EntityUpdated(ftSignaturCreationUnitGR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitGR entity) => entity.ftSignaturCreationUnitGRId;
    }
}
