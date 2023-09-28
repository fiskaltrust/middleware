using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.EF.Repositories.FR
{
    public class EfJournalFRCopyPayloadRepository : IJournalFRCopyPayloadRepository
    {
        private readonly DbContext _dbContext;

        public EfJournalFRCopyPayloadRepository(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference)
        {
            return await _dbContext.Set<ftJournalFRCopyPayload>().CountAsync(x => x.CopiedReceiptReference == cbPreviousReceiptReference);
        }

        public async Task<bool> InsertAsync(ftJournalFRCopyPayload entity)
        {
            var id = entity.QueueItemId;
            if (await _dbContext.Set<ftJournalFRCopyPayload>().AnyAsync(e => e.QueueItemId == id))
            {
                throw new Exception($"Entity with id {id} already exists");
            }

            EntityUpdated(entity);

            _dbContext.Set<ftJournalFRCopyPayload>().Add(entity);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> HasEntriesAsync()
        {
            return await _dbContext.Set<ftJournalFRCopyPayload>().AnyAsync();
        }

        protected void EntityUpdated(ftJournalFRCopyPayload entity)
        {
            entity.TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}