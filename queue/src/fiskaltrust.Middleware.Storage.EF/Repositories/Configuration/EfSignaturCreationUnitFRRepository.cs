using System;
using fiskaltrust.Middleware.Storage.EF.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.Configuration
{
    public class EfSignaturCreationUnitFRRepository : AbstractEFRepostiory<Guid, ftSignaturCreationUnitFR>
    {
        public EfSignaturCreationUnitFRRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftSignaturCreationUnitFR entity)=> entity.TimeStamp = DateTime.UtcNow.Ticks;
        
        protected override Guid GetIdForEntity(ftSignaturCreationUnitFR entity)=> entity.ftSignaturCreationUnitFRId;    
    }
}