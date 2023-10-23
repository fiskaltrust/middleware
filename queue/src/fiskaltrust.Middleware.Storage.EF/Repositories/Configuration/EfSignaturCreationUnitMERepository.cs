using System;
using fiskaltrust.Middleware.Storage.EF.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.Configuration
{
    public class EfSignaturCreationUnitMERepository : AbstractEFRepostiory<Guid, ftSignaturCreationUnitME>
    {
        public EfSignaturCreationUnitMERepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftSignaturCreationUnitME entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;

        protected override Guid GetIdForEntity(ftSignaturCreationUnitME entity) => entity.ftSignaturCreationUnitMEId;
    }
}