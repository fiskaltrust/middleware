using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using fiskaltrust.Middleware.Contracts.Exceptions;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Receipt;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Invoice;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.DailyOperations;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Lifecycle;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Log;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public interface IReceiptTypeProcessor
    {
        public ITReceiptCases ReceiptCase { get; }

        public bool FailureModeAllowed { get; }

        public bool GenerateJournalIT { get; }

        Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem);
    }
}
