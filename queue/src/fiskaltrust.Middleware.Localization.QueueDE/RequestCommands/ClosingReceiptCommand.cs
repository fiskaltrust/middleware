using System;
using System.Collections.Generic;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.Middleware.Contracts.Models;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    internal abstract class ClosingReceiptCommand : RequestCommand
    {
        protected readonly IMasterDataService _masterDataService;

        protected ClosingReceiptCommand(IMasterDataService masterDataService, ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, 
            IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, 
            IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration,
            IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, 
            IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService) 
            : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository, 
                  middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService)
        {
            _masterDataService = masterDataService;
        }

        protected static List<ftActionJournal> CreateClosingActionJournals(ftQueueItem queueItem, ftQueue queue, ulong transactionNumber, bool masterDataChanged, string message, long type, int? closingNumber = null)
        {
            return new List<ftActionJournal>
            {
                new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    Message = message,
                    Type = $"{type:X}",
                    ftQueueId = queue.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    TimeStamp = DateTime.UtcNow.Ticks,
                    Priority = -1,
                    DataJson = JsonConvert.SerializeObject(new
                    {
                        ftReceiptNumerator = queue.ftReceiptNumerator + 1,
                        transactionNumber = transactionNumber,
                        masterDataChanged = masterDataChanged,
                        closingNumber = closingNumber ?? -1
                    })
                }
            };
        }
    }
}
