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
using System;

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

        public async Task<HashSet<ReceiptReferencesGroupedData>> GetReceiptReferenceAsync(long from, long to)
        {
            var receiptReferencesGrouped = _middlewareQueueItemRepository.GetGroupedReceiptReferenceAsync(from, to);
            var receiptReferences = new HashSet<ReceiptReferencesGroupedData>();
            await foreach (var receiptReference in receiptReferencesGrouped)
            {
                var row = 0;
                var selected = new ftQueueItem();
                await foreach (var queueItem in _middlewareQueueItemRepository.GetQueueItemsForReceiptReferenceAsync(receiptReference))
                {
                    if (row == 0)
                    {
                        selected = queueItem;
                        row++;
                        continue;
                    }
                    await AddReference(receiptReferences, queueItem, selected);
                    var source = await _middlewareQueueItemRepository.GetClosestPreviousReceiptReferencesAsync(queueItem);
                    await AddReference(receiptReferences, queueItem, source);

                    selected = queueItem;
                    row++;
                }
                if (row == 1 && !string.IsNullOrEmpty(selected.ftQueueItemId.ToString()))
                {
                    var source = await _middlewareQueueItemRepository.GetClosestPreviousReceiptReferencesAsync(selected);
                    await AddReference(receiptReferences, selected, source);
                }
            }
            return receiptReferences;
        }

        public Task<HashSet<ReceiptReferenceData>> GetReceiptReferenceAsync(ftQueueItem queueItem) => throw new NotImplementedException();

        private async Task<bool> AddReference(HashSet<ReceiptReferencesGroupedData> receiptReferences, ftQueueItem target, ftQueueItem source)
        {
            if (target == null || string.IsNullOrEmpty(target.response))
            {
                return false;
            }
            (var zNrTarget, var zErstTarget) = await GetLastZNumberForQueueItem(target).ConfigureAwait(false);
            //external references
            if (source == null)
            {
                var requestTarget = JsonConvert.DeserializeObject<ReceiptRequest>(target.request);
                if (string.IsNullOrEmpty(requestTarget.ftReceiptCaseData))
                {
                    return false;
                }
                var receiptCaseData = SerializationHelper.GetReceiptCaseData(requestTarget);
                if (receiptCaseData == null || string.IsNullOrEmpty(receiptCaseData.RefReceiptId))
                {
                    return false;
                }
                var respTarget = JsonConvert.DeserializeObject<ReceiptResponse>(target.response);

                var extReceiptReference = new ReceiptReferencesGroupedData()
                {
                    TargetQueueItemId = target.ftQueueItemId,
                    TargetReceiptCaseData = receiptCaseData,
                    TargetReceiptIdentification = respTarget.ftReceiptIdentification,
                    TargetZNumber = zNrTarget
                };

                if (zErstTarget.HasValue)
                {
                    extReceiptReference.TargetZErstellung = zErstTarget.Value;
                }
                return receiptReferences.Add(extReceiptReference);
            }
            if (string.IsNullOrEmpty(source.response))
            {
                return false;
            }

            var responseTarget = JsonConvert.DeserializeObject<ReceiptResponse>(target.response);
            var responseSource = JsonConvert.DeserializeObject<ReceiptResponse>(source.response);

            (var znrSource, _) = await GetLastZNumberForQueueItem(source).ConfigureAwait(false);

            var receiptReference = new ReceiptReferencesGroupedData()
            {
                RefMoment = source.cbReceiptMoment,
                RefReceiptId = responseSource.ftReceiptIdentification,
                TargetQueueItemId = target.ftQueueItemId,
                ZNumber = znrSource,
                SourceQueueItemId = source.ftQueueItemId,
                TargetReceiptIdentification = responseTarget.ftReceiptIdentification,
                TargetZNumber = zNrTarget,
                RefName = source.cbReceiptReference
            };

            if (zErstTarget.HasValue)
            {
                receiptReference.TargetZErstellung = zErstTarget.Value;
            }
            return receiptReferences.Add(receiptReference);
        }

        private async Task<(long, DateTime?)> GetLastZNumberForQueueItem(ftQueueItem queueItem)
        {
            var actionJournals = (await _actionJournalRepository.GetAsync()).Where(x => x.Type == "4445000000000007").OrderBy(x => x.TimeStamp);
            var actionJournal = actionJournals.Where(x => x.TimeStamp > queueItem.TimeStamp).FirstOrDefault();
            if (actionJournal == null)
            {
                return (-1, null);
            }
            var closingNumber = JsonConvert.DeserializeAnonymousType(actionJournal.DataJson, new { closingNumber = -1 }).closingNumber;

            return closingNumber > -1
                ? (closingNumber, actionJournal.Moment)
                : (actionJournals.Where(x => x.Type == "4445000000000007" & x.TimeStamp <= queueItem.TimeStamp).Count(), actionJournals.Where(x => x.Type == "4445000000000007" & x.TimeStamp <= queueItem.TimeStamp).Last()?.Moment);
        }


    }
}
