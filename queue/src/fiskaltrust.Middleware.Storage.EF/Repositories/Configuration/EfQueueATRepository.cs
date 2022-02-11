using System;
using fiskaltrust.Middleware.Storage.EF.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.Configuration
{
    public class EfQueueATRepository : AbstractEFRepostiory<Guid, ftQueueAT>
    {
        public EfQueueATRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftQueueAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftQueueAT entity)=> entity.ftQueueATId;
    }
}
