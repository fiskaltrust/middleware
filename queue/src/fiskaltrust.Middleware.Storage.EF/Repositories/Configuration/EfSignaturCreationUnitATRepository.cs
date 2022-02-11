using System;
using fiskaltrust.Middleware.Storage.EF.Repositories;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.Configuration
{
    public class EfSignaturCreationUnitATRepository : AbstractEFRepostiory<Guid, ftSignaturCreationUnitAT>
    {
        public EfSignaturCreationUnitATRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftSignaturCreationUnitAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;
        
        protected override Guid GetIdForEntity(ftSignaturCreationUnitAT entity)=> entity.ftSignaturCreationUnitATId;
    }
}
    
