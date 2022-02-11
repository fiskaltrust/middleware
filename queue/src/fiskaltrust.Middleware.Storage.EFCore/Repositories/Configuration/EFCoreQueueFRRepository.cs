using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration
{
    public class EFCoreQueueFRRepository : AbstractEFCoreRepostiory<Guid, ftQueueFR>
    {
        public EFCoreQueueFRRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftQueueFR entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueFR entity) => entity.ftQueueFRId;
    }
}

