using System;
using fiskaltrust.Middleware.Storage.EF.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.Configuration
{
    public class EfQueueRepository : AbstractEFRepostiory<Guid, ftQueue>
    {
        public EfQueueRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftQueue entity)=> entity.TimeStamp = DateTime.UtcNow.Ticks;    

        protected override Guid GetIdForEntity(ftQueue entity)=> entity.ftQueueId;
       
    }
}
