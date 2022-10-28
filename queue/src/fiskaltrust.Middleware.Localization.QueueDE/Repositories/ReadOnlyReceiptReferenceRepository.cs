using fiskaltrust.Exports.Common.Models;
using fiskaltrust.Exports.Common.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Exports.Common.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<HashSet<ReceiptReferenceData>> GetReceiptReferenceAsync(long from, long to)
        {
            var receiptReferencesGrouped = _middlewareQueueItemRepository.GetGroupedReceiptReference(from, to);
            var receiptReferences = new HashSet<ReceiptReferenceData>();
            await foreach (var receiptReference in receiptReferencesGrouped)
            {
                var row = 0;
                var source = new ftQueueItem();
                await foreach (var queueItem in _middlewareQueueItemRepository.GetQueueItemsForReceiptReference(receiptReference))
                {
                    if (row == 0)
                    {
                        source = queueItem;
                        row++;
                        continue;
                    }
                    await AddReference(receiptReferences, source, queueItem);
                    var target = await _middlewareQueueItemRepository.GetFirstPreviousReceiptReferencesAsync(queueItem);
                    await AddReference(receiptReferences, source, target);

                    source = queueItem;
                    row++;
                }
                if (row == 1 && !string.IsNullOrEmpty(source.ftQueueItemId.ToString()))
                {
                    var target = await _middlewareQueueItemRepository.GetFirstPreviousReceiptReferencesAsync(source);
                    await AddReference(receiptReferences, source, target);
                }
            }
            return receiptReferences;
        }

        private async Task<bool> AddReference(HashSet<ReceiptReferenceData> receiptReferences, ftQueueItem source, ftQueueItem target)
        {
            if (source == null || target == null || string.IsNullOrEmpty(source.response))
            {
                return false;
            }
            var responseTarget = JsonConvert.DeserializeObject<ReceiptResponse>(target.response);

            var requestSource = JsonConvert.DeserializeObject<ReceiptRequest>(source.request);
            var responseSource = JsonConvert.DeserializeObject<ReceiptResponse>(source.response);
            var receiptCaseDataSource = SerializationHelper.GetReceiptCaseData(requestSource);

            var znumber = await GetLastZNumberForQueueItem(_actionJournalRepository, target).ConfigureAwait(false);

            return receiptReferences.Add(new ReceiptReferenceData()
            {
                TargetRefMoment = target.cbReceiptMoment,
                TargetRefReceiptId = responseTarget.ftReceiptIdentification,
                TargetQueueItemId = target.ftQueueItemId,
                TargetZNumber = znumber,
                SourceQueueItemId = source.ftQueueItemId,
                SourceReceiptCaseData = receiptCaseDataSource,
                SouceReceiptIdentification = responseSource.ftReceiptIdentification,
            });
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
