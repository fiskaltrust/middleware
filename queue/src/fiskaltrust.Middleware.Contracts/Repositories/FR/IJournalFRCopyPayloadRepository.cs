﻿using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.Models.FR;

namespace fiskaltrust.Middleware.Contracts.Repositories.FR
{
    public interface IJournalFRCopyPayloadRepository
    {
        Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference);
        Task<bool> InsertAsync(ftJournalFRCopyPayload c);
    }
}