using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Exports.Common.Models;
using fiskaltrust.Exports.Common.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;


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
                var closingNumber = JsonConvert.DeserializeAnonymousType(aj.DataJson, new { closingNumber = 0L }).closingNumber;
                var queueItem = await _queueItemRepository.GetAsync(aj.ftQueueItemId).ConfigureAwait(false);
                queueItem = queueItem ?? await GetQueueItemOfMissingIdAsync(aj, _queueItemRepository);
                var dailyClosingReceipt = new DailyClosingReceipt
                {
                    ZNumber = closingNumber,
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

        public static async Task<ftQueueItem> GetQueueItemOfMissingIdAsync(ftActionJournal actionJournal, IMiddlewareQueueItemRepository queueItemRepository)
        {
            var from = actionJournal.Moment.AddMinutes(-5).Ticks;
            var to = actionJournal.Moment.AddMinutes(5).Ticks;
            var closequeueItems = queueItemRepository.GetByTimeStampRangeAsync(from, to);
            var ordered = closequeueItems.OrderByDescending(x => x.ftQueueMoment.Ticks).ToListAsync();
            var queueItemOfMissingId = await closequeueItems.OrderByDescending(x => x.ftQueueMoment.Ticks).Where(x => x.ftQueueMoment.Ticks < actionJournal.Moment.Ticks).FirstOrDefaultAsync();
            return queueItemOfMissingId;
        }
    }
}
