using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration
{
    public class EFCoreSignaturCreationUnitDERepository : AbstractEFCoreRepostiory<Guid, ftSignaturCreationUnitDE>
    {
        public EFCoreSignaturCreationUnitDERepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftSignaturCreationUnitDE entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitDE entity) => entity.ftSignaturCreationUnitDEId;
    }
}