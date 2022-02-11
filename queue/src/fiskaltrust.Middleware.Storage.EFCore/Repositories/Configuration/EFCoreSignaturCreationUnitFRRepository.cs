using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.Configuration
{
    public class EFCoreSignaturCreationUnitFRRepository : AbstractEFCoreRepostiory<Guid, ftSignaturCreationUnitFR>
    {
        public EFCoreSignaturCreationUnitFRRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override void EntityUpdated(ftSignaturCreationUnitFR entity)=> entity.TimeStamp = DateTime.UtcNow.Ticks;
        
        protected override Guid GetIdForEntity(ftSignaturCreationUnitFR entity)=> entity.ftSignaturCreationUnitFRId;    
    }
}