using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemorySignaturCreationUnitATRepository : AbstractInMemoryRepository<Guid, ftSignaturCreationUnitAT>
    {
        public InMemorySignaturCreationUnitATRepository() : base(new List<ftSignaturCreationUnitAT>()) { }

        public InMemorySignaturCreationUnitATRepository(IEnumerable<ftSignaturCreationUnitAT> data) : base(data) { }

        protected override void EntityUpdated(ftSignaturCreationUnitAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitAT entity) => entity.ftSignaturCreationUnitATId;
    }
}
