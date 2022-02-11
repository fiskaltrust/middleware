using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration
{
    public class EFCoreQueueRepository : AbstractEFCoreRepostiory<Guid, ftQueue>
    {
        public EFCoreQueueRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftQueue entity)=> entity.TimeStamp = DateTime.UtcNow.Ticks;    

        protected override Guid GetIdForEntity(ftQueue entity)=> entity.ftQueueId;
       
    }
}
