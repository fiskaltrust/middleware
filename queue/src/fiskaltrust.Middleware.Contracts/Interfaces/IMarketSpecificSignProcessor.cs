﻿using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Contracts.Interfaces
{
    public interface IMarketSpecificSignProcessor
    {
        Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem);
        Task<string> GetFtCashBoxIdentificationAsync(ftQueue queue);
    }
}