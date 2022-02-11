using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.InMemory.Repositories.Configuration
{
    public class InMemoryCashBoxRepository : AbstractInMemoryRepository<Guid, ftCashBox>
    {
        public InMemoryCashBoxRepository() : base(new List<ftCashBox>()) { }

        public InMemoryCashBoxRepository(IEnumerable<ftCashBox> data) : base(data) { }

        protected override void EntityUpdated(ftCashBox entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftCashBox entity) => entity.ftCashBoxId;
    }
}
