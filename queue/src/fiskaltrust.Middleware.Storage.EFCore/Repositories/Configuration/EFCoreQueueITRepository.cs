using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration
{
    public class EFCoreQueueITRepository : AbstractEFCoreRepostiory<Guid, ftQueueIT>
    {
        public EFCoreQueueITRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftQueueIT entity)=> entity.TimeStamp = DateTime.UtcNow.Ticks;
        
        protected override Guid GetIdForEntity(ftQueueIT entity)=> entity.ftQueueITId;
    }    
}
