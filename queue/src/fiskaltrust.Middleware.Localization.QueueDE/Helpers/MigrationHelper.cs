using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.Models;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.Helpers
{
    internal class MigrationHelper
    {
        public static readonly string Meassage = "Migration to another machine or the CloudCashbox started, no further receipts can be sent to this installation of the Middleware.";
        public static readonly string ExceptionMessage = "The Middleware is currently in migration mode after a 'start migration' receipt was processed. Please continue the migration process in the Portal and use the migrated instance of the Middleware to continue signing, either on another machine or in the CloudCashbox.";
        public static async Task FinishMigrationAync(ftQueue queue, ftQueueItem queueItem, IMiddlewareActionJournalRepository actionJournalRepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareReceiptJournalRepository receiptJournalRepositor, IMiddlewareJournalDERepository journalDERepository)
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
                Message = Meassage,
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

        public static bool IsMigrationInProgress(IMiddlewareQueueItemRepository queueItemRepository)
        {
            var queueItem = queueItemRepository.GetLastQueueItemAsync().Result;
            if(queueItem == null)
            {
                return false;
            }
            var request = JsonConvert.DeserializeObject<ReceiptRequest>(queueItem.request);
            if ((request.ftReceiptCase & 0xFFFF) == 0x0019)
            {
                return true;
            }
            return false;
        }
    }
}
