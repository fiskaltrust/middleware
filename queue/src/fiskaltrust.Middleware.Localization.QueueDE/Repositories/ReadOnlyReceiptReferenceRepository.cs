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

        public ReadOnlyReceiptReferenceRepository(IMiddlewareQueueItemRepository middlewareQueueItemRepository)
        {
            _middlewareQueueItemRepository = middlewareQueueItemRepository;
        }

        public async Task<HashSet<ReceiptReferencesGroupedData>> GetReceiptReferenceAsync(long from, long to, List<DailyClosingReceipt> dailyClosings)
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
                    AddReference(receiptReferences, queueItem, selected, dailyClosings);
                    var source = await _middlewareQueueItemRepository.GetClosestPreviousReceiptReferencesAsync(queueItem);
                    AddReference(receiptReferences, queueItem, source, dailyClosings);

                    selected = queueItem;
                    row++;
                }
                if (row == 1 && !string.IsNullOrEmpty(selected.ftQueueItemId.ToString()))
                {
                    var source = await _middlewareQueueItemRepository.GetClosestPreviousReceiptReferencesAsync(selected);
                    AddReference(receiptReferences, selected, source, dailyClosings);
                }
            }
            return receiptReferences;
        }

        public Task<HashSet<ReceiptReferenceData>> GetReceiptReferenceAsync(ftQueueItem queueItem) => throw new NotImplementedException();

        private bool AddReference(HashSet<ReceiptReferencesGroupedData> receiptReferences, ftQueueItem target, ftQueueItem source, List<DailyClosingReceipt> dailyClosings)
        {
            if (target == null || string.IsNullOrEmpty(target.response))
            {
                return false;
            }
            var dailyClosingTarget = dailyClosings.Where(x => x.ZTime >= target.cbReceiptMoment).FirstOrDefault();

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
                    TargetZNumber = dailyClosingTarget.ZNumber,
                    TargetZErstellung = dailyClosingTarget.ZTime
                };

                return receiptReferences.Add(extReceiptReference);
            }
            if (string.IsNullOrEmpty(source.response))
            {
                return false;
            }

            var responseTarget = JsonConvert.DeserializeObject<ReceiptResponse>(target.response);
            var responseSource = JsonConvert.DeserializeObject<ReceiptResponse>(source.response);

            var dailyClosingSource = dailyClosings.Where(x => x.ZTime >= source.cbReceiptMoment).FirstOrDefault();

            var receiptReference = new ReceiptReferencesGroupedData()
            {
                RefMoment = dailyClosingSource.ZTime,
                RefReceiptId = responseSource.ftReceiptIdentification,
                TargetQueueItemId = target.ftQueueItemId,
                ZNumber = dailyClosingSource.ZNumber,
                SourceQueueItemId = source.ftQueueItemId,
                TargetReceiptIdentification = responseTarget.ftReceiptIdentification,
                TargetZNumber = dailyClosingTarget.ZNumber,
                TargetZErstellung = dailyClosingTarget.ZTime
            };

            return receiptReferences.Add(receiptReference);
        }
    }
}
