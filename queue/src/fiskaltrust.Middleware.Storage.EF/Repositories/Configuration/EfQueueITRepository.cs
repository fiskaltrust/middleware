using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.Configuration
{
    public class EfQueueITRepository : AbstractEFRepostiory<Guid, ftQueueIT>
    {
        public EfQueueITRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftQueueIT entity)=> entity.TimeStamp = DateTime.UtcNow.Ticks;
        
        protected override Guid GetIdForEntity(ftQueueIT entity)=> entity.ftQueueITId;
    }    
}
