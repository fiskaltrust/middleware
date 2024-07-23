using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Exports.Common.Models;
using fiskaltrust.Exports.Common.Repositories;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace fiskaltrust.Middleware.Localization.QueueDE.Repositories
{
    public class DailyClosingRepository : IDailyClosingRepository
    {
        private readonly IReadOnlyActionJournalRepository _actionJournalRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;


        public DailyClosingRepository(IReadOnlyActionJournalRepository actionJournalRepository, IMiddlewareQueueItemRepository queueItemRepository)
        {
            _actionJournalRepository = actionJournalRepository;
            _queueItemRepository = queueItemRepository;
        }

        public async Task<List<DailyClosingReceipt>> GetAsync()
        {
            var dailyClosingReceipts = new List<DailyClosingReceipt>();
            var ajs = await _actionJournalRepository.GetAsync().ConfigureAwait(false);
            var dailyClosings = ajs.OrderBy(x => x.Moment).Where(x => x.Type != null && x.Type.EndsWith("7") && (x.Type.StartsWith("4445") || x.Type.StartsWith("0x4445")));

            foreach (var aj in dailyClosings)
            {
                var queueItem = aj.ftQueueItemId == aj.ftQueueId
                    ? await GetQueueItemOfMissingIdAsync(aj).ConfigureAwait(false)
                    : await _queueItemRepository.GetAsync(aj.ftQueueItemId).ConfigureAwait(false);

                var closingNumber = JsonConvert.DeserializeAnonymousType(aj.DataJson, new JObject()).Property("closingNumber")?.Value.Value<long?>();
                if(closingNumber == null)
                {
                    var response = JsonConvert.DeserializeObject<ReceiptResponse>(queueItem.response);
                    var currentStateData = new JObject();
                    if (!string.IsNullOrEmpty(response.ftStateData))
                    {
                        currentStateData = (JObject) JsonConvert.DeserializeObject(response.ftStateData);
                    }
                        
                    closingNumber = currentStateData.Property("DailyClosingNumber")?.Value.Value<long>() ?? 0L;
                }

                var dailyClosingReceipt = new DailyClosingReceipt
                {
                    ZNumber = (long)closingNumber,
                };
                if (queueItem != null)
                {
                    dailyClosingReceipt.QueueRow = queueItem.ftQueueRow;
                    dailyClosingReceipt.ZTime = queueItem.cbReceiptMoment;
                }
                dailyClosingReceipts.Add(dailyClosingReceipt);
            }
            return dailyClosingReceipts;
        }

        // In the MW 1.3.53, an issue was introduced that lead to the ftQueueItemId being set to the ftQueueId in some cases.
        // This only happened when the Queue was in failed mode during processing the daily closing, and is hence relatively rare.
        public async Task<ftQueueItem> GetQueueItemOfMissingIdAsync(ftActionJournal actionJournal)
        {
            var receiptNumerator = JsonConvert.DeserializeAnonymousType(actionJournal.DataJson, new { ftReceiptNumerator = 0L }).ftReceiptNumerator;

            // QueueItems are stored before the ActionJournals, so we need to look for the QueueItem with the closest timestamp before the respective ActionJournal
            var to = new DateTime(actionJournal.TimeStamp).AddMinutes(1).Ticks;
            var queueItemsInRange = _queueItemRepository.GetByTimeStampRangeAsync(actionJournal.TimeStamp, to);

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
