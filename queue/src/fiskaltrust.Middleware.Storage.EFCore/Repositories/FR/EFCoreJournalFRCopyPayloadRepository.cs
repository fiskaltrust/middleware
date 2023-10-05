using fiskaltrust.Middleware.Contracts.Models.FR;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Storage.EFCore.Repositories.FR
{
    public class EFCoreJournalFRCopyPayloadRepository : AbstractEFCoreRepostiory<Guid, ftJournalFRCopyPayload>, IJournalFRCopyPayloadRepository
    {
        public EFCoreJournalFRCopyPayloadRepository(MiddlewareDbContext dbContext) : base(dbContext) { }

        protected override Guid GetIdForEntity(ftJournalFRCopyPayload entity) => entity.QueueItemId;

        public async Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference)
        {
            return await DbContext.FtJournalFRCopyPayloads.CountAsync(x => x.CopiedReceiptReference == cbPreviousReceiptReference);
        }

        protected override void EntityUpdated(ftJournalFRCopyPayload entity)
        {
            entity.TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}