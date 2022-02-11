using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Exports.Common.Models;
using fiskaltrust.Exports.Common.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueDE.Repositories
{
    public class ReadOnlyReceiptReferenceRepository : IReadOnlyReceiptReferenceRepository
    {
        private readonly IMiddlewareQueueItemRepository _middlewareQueueItemRepository;
        private readonly IReadOnlyActionJournalRepository _actionJournalRepository;

        public ReadOnlyReceiptReferenceRepository(IMiddlewareQueueItemRepository middlewareQueueItemRepository, IReadOnlyActionJournalRepository actionJournalRepository)
        {
            _middlewareQueueItemRepository = middlewareQueueItemRepository;
            _actionJournalRepository = actionJournalRepository;
        }

        public async Task<HashSet<ReceiptReferenceData>> GetReceiptReferenceAsync(ftQueueItem queueItem)
        {
            var previousReceiptReferences = _middlewareQueueItemRepository.GetPreviousReceiptReferencesAsync(queueItem);
            var receiptReferences = new HashSet<ReceiptReferenceData>();
            await foreach (var item in previousReceiptReferences)
            {
                if (string.IsNullOrEmpty(item.response))
                {
                    continue;
                }
                var response = JsonConvert.DeserializeObject<ReceiptResponse>(item.response);
                var znumber = await GetLastZNumberForQueueItem(_actionJournalRepository, item).ConfigureAwait(false);
                _ = receiptReferences.Add(new ReceiptReferenceData()
                {
                    RefMoment = item.cbReceiptMoment,
                    RefReceiptId = response.ftReceiptIdentification,
                    TargetQueueItemId = item.ftQueueItemId,
                    ZNumber = znumber
                });
            }
            return receiptReferences;
        }

        private static async Task<long> GetLastZNumberForQueueItem(IReadOnlyActionJournalRepository actionJournalRepository, ftQueueItem queueItem)
        {
            var actionJournals = (await actionJournalRepository.GetAsync()).Where(x => x.Type == "4445000000000007").OrderBy(x => x.TimeStamp);
            var actionJournal = actionJournals.Where(x => x.TimeStamp > queueItem.TimeStamp).FirstOrDefault();
            if (actionJournal == null)
            {
                return -1;
            }
            var closingNumber = JsonConvert.DeserializeAnonymousType(actionJournal.DataJson, new { closingNumber = -1 }).closingNumber;
            return closingNumber > -1
                ? closingNumber
                : actionJournals.Where(x => x.Type == "4445000000000007" & x.TimeStamp <= queueItem.TimeStamp).Count();
        }
    }
}
