using System;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.FR;

namespace fiskaltrust.Middleware.Contracts.Repositories.FR
{
    public interface IJournalFRCopyPayloadRepository
    {
        Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference);
        Task InsertAsync(ftJournalFRCopyPayload c);
        Task<ftJournalFRCopyPayload> GetAsync(Guid queueItemId);
        Task InsertAsync(storage.V0.ftJournalFRCopyPayload ftJournalFRCopyPayload);
        Task InsertAsync(storage.V0.ftJournalFRCopyPayload ftJournalFRCopyPayload);
    }
}