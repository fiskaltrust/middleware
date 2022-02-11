using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration
{
    public class EFCoreCashBoxRepository : AbstractEFCoreRepostiory<Guid, ftCashBox>
    {
        public EFCoreCashBoxRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftCashBox entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftCashBox entity)=> entity.ftCashBoxId;
    }
}
