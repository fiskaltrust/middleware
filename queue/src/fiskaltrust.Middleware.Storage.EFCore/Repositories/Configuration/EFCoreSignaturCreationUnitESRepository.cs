using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration
{
    public class EFCoreSignaturCreationUnitESRepository : AbstractEFCoreRepostiory<Guid, ftSignaturCreationUnitES>
    {
        public EFCoreSignaturCreationUnitESRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftSignaturCreationUnitES entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitES entity) => entity.ftSignaturCreationUnitESId;
    }
}