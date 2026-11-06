using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.FR;

namespace fiskaltrust.Middleware.Contracts.Repositories.FR
{
    public interface IJournalFRCopyPayloadRepository
    {
        Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference);

        /// <summary>The copies journaled for one copied receipt reference, in no particular order.</summary>
        Task<IEnumerable<ftJournalFRCopyPayload>> GetCopiesAsync(string copiedReceiptReference);
        Task InsertAsync(ftJournalFRCopyPayload c);
        Task<ftJournalFRCopyPayload> GetAsync(Guid queueItemId);
    }
}