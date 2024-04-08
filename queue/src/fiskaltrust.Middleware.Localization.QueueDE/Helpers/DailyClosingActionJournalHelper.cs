using System;
using System.Globalization;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueDE.Helpers
{
    public class DailyClosingActionJournalHelper
    {
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;

        public DailyClosingActionJournalHelper(IMiddlewareQueueItemRepository queueItemRepository) => _queueItemRepository = queueItemRepository;

        // In the MW 1.3.53, an issue was introduced that lead to the ftQueueItemId being set to the ftQueueId in some cases.
        // This only happened when the Queue was in failed mode during processing the daily closing, and is hence relatively rare.
        public async Task<ftQueueItem> GetQueueItemOfMissingIdAsync(ftActionJournal actionJournal)
        {
            var receiptNumerator = JsonConvert.DeserializeAnonymousType(actionJournal.DataJson, new { ftReceiptNumerator = 0L }).ftReceiptNumerator;

            // QueueItems are stored before the ActionJournals, so we need to look for the QueueItem with the closest timestamp before the respective ActionJournal
            var from = new DateTime(actionJournal.TimeStamp).AddSeconds(-10).Ticks;
            var queueItemsInRange = _queueItemRepository.GetByTimeStampRangeAsync(from, actionJournal.TimeStamp);

            await foreach (var queueItem in queueItemsInRange)
            {
                if (queueItem.response != null)
                {
                    var response = JsonConvert.DeserializeObject<ReceiptResponse>(queueItem.response);
                    if (GetReceiptNumerator(response.ftReceiptIdentification) == receiptNumerator)
                    {
                        return queueItem;
                    }
                }
            }
            return null;
        }

        private long GetReceiptNumerator(string ftReceiptIdentification)
        {
            var endIndex = ftReceiptIdentification.IndexOf("#");
            var numeratorString = ftReceiptIdentification.Substring(2, endIndex - 2);
            return long.Parse(numeratorString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
    }
}