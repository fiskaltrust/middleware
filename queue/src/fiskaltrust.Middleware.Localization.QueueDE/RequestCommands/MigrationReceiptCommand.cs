using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.Middleware.Localization.QueueDE.Services;
using fiskaltrust.Middleware.Localization.QueueDE.Transactions;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.RequestCommands
{
    internal class MigrationReceiptCommand : DailyClosingReceiptCommand
    {
        public override string ReceiptName => "Migration receipt";

        public MigrationReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryDE signatureFactory, IDESSCDProvider deSSCDProvider, ITransactionPayloadFactory transactionPayloadFactory, IReadOnlyQueueItemRepository queueItemRepository, IConfigurationRepository configurationRepository, IJournalDERepository journalDERepository, MiddlewareConfiguration middlewareConfiguration, IPersistentTransactionRepository<FailedStartTransaction> failedStartTransactionRepo, IPersistentTransactionRepository<FailedFinishTransaction> failedFinishTransactionRepo, IPersistentTransactionRepository<OpenTransaction> openTransactionRepo, ITarFileCleanupService tarFileCleanupService, QueueDEConfiguration queueDEConfiguration, IMasterDataService masterDataService) : base(logger, signatureFactory, deSSCDProvider, transactionPayloadFactory, queueItemRepository, configurationRepository, journalDERepository, middlewareConfiguration, failedStartTransactionRepo, failedFinishTransactionRepo, openTransactionRepo, tarFileCleanupService, queueDEConfiguration, masterDataService)
        {
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueDE queueDE, ReceiptRequest request, ftQueueItem queueItem)
        {
            ThrowIfHasChargeOrPayItems(request);

            var requestCommandResponse = await base.ExecuteAsync(queue, queueDE, request, queueItem);
            requestCommandResponse.isMigration = true;
            return requestCommandResponse;
        }

        public static async Task FinishMigration(ftQueue queue, ftQueueItem queueItem, IMiddlewareActionJournalRepository actionJournalRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareReceiptJournalRepository receiptJournalRepositor, IMiddlewareJournalDERepository journalDERepository)
        {
            var countActionJournal = await actionJournalRepository.CountAsync().ConfigureAwait(false);
            var countQueueItem = await queueItemRepository.CountAsync().ConfigureAwait(false);
            var countReceiptJournal = await receiptJournalRepositor.CountAsync().ConfigureAwait(false);
            var countJournalDE = await journalDERepository.CountAsync().ConfigureAwait(false);

            var migrationState = new MigrationState()
            {
                ActionJournalCount = countActionJournal,
                QueueItemCount = countQueueItem,
                ReceiptJournalCount = countReceiptJournal,
                JournalDECount = countJournalDE,
                QueueRow = queue.ftQueuedRow
            };

            var actionJournal = new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                Message = "Migration done, no further receipts can be sent to the middleware.",
                Type = $"{0x4445000000000019:X}",
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Moment = DateTime.UtcNow,
                TimeStamp = DateTime.UtcNow.Ticks,
                Priority = 0,
                DataJson = JsonConvert.SerializeObject(migrationState)
            };
            
            await actionJournalRepository.InsertAsync(actionJournal).ConfigureAwait(false);
        }

        public static bool IsMigrationDone(IMiddlewareQueueItemRepository queueItemRepository)
        {
            var queueItem = queueItemRepository.GetLastQueueItem().Result;
            if(queueItem == null)
            {
                return false;
            }
            var request = JsonConvert.DeserializeObject<ReceiptRequest>(queueItem.request);
            if (request.ftReceiptCase == 0x4445000000000019)
            {
                return true;
            }
            return false;
        }
    }
}
