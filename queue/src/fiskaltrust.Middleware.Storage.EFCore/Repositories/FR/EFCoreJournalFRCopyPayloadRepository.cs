using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Storage.EFCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.FR
{
    public class EFCoreJournalFRCopyPayloadRepository : AbstractEFCoreRepostiory<Guid, ftJournalFRCopyPayload>, IJournalFRCopyPayloadRepository
    {
        public EFCoreJournalFRCopyPayloadRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override Guid GetIdForEntity(ftJournalFRCopyPayload entity) => entity.QueueItemId;

        public async Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference)
        {
            return await DbContext.Set<ftJournalFRCopyPayload>().CountAsync(x => x.CopiedReceiptReference == cbPreviousReceiptReference);
        }

        public new async Task<bool> InsertAsync(ftJournalFRCopyPayload entity)
        {
            var id = GetIdForEntity(entity);
            if (await DbContext.Set<ftJournalFRCopyPayload>().AnyAsync(e => e.QueueItemId == id))
            {
                throw new Exception($"Entity with id {id} already exists");
            }
            
            EntityUpdated(entity);
            
            DbContext.Set<ftJournalFRCopyPayload>().Add(entity);
            await DbContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> HasEntriesAsync()
        {
            return await DbContext.Set<ftJournalFRCopyPayload>().AnyAsync();
        }

        protected override void EntityUpdated(ftJournalFRCopyPayload entity)
        {
            entity.TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}