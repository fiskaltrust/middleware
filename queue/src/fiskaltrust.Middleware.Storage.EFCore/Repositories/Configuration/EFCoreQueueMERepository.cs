using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration
{
    public class EFCoreQueueMERepository : AbstractEFCoreRepostiory<Guid, ftQueueME>
    {
        public EFCoreQueueMERepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftQueueME entity)=> entity.TimeStamp = DateTime.UtcNow.Ticks;
        
        protected override Guid GetIdForEntity(ftQueueME entity)=> entity.ftQueueMEId;
    }    
}
