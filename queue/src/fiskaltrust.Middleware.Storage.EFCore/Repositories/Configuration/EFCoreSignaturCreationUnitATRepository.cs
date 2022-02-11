using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration
{
    public class EFCoreSignaturCreationUnitATRepository : AbstractEFCoreRepostiory<Guid, ftSignaturCreationUnitAT>
    {
        public EFCoreSignaturCreationUnitATRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftSignaturCreationUnitAT entity) => entity.TimeStamp = DateTime.UtcNow.Ticks;
        
        protected override Guid GetIdForEntity(ftSignaturCreationUnitAT entity)=> entity.ftSignaturCreationUnitATId;
    }
}
    
