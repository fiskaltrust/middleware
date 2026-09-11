using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemorySignaturCreationUnitPLRepository : AbstractInMemoryRepository<Guid, ftSignaturCreationUnitPL>
    {
        public InMemorySignaturCreationUnitPLRepository() : base(new List<ftSignaturCreationUnitPL>()) { }

        public InMemorySignaturCreationUnitPLRepository(IEnumerable<ftSignaturCreationUnitPL> data) : base(data) { }

        protected override void EntityUpdated(ftSignaturCreationUnitPL entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitPL entity) => entity.ftSignaturCreationUnitPLId;
    }
}
