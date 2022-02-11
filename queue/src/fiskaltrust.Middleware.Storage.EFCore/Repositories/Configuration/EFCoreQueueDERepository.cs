using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration
{
    public class EFCoreQueueDERepository : AbstractEFCoreRepostiory<Guid, ftQueueDE>
    {
        public EFCoreQueueDERepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftQueueDE entity)=> entity.TimeStamp = DateTime.UtcNow.Ticks;
        
        protected override Guid GetIdForEntity(ftQueueDE entity)=> entity.ftQueueDEId;
    }    
}
