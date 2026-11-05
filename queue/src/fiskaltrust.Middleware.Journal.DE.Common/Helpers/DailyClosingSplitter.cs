using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;
using fiskaltrust.Middleware.Journal.DE.Common.Repositories;
using fiskaltrust.ifPOS.v1;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Journal.DE.Common.Helpers
{
    public class DailyClosingSplitter
    {
        private readonly IReadOnlyReceiptJournalRepository _receiptJournalRepository;
        private readonly IReadOnlyQueueItemRepository _queueItemRepository;
        private readonly IReadOnlyReceiptReferenceRepository _receiptReferenceRepository;
        private readonly IDailyClosingRepository _dailyClosingRepository;
        private readonly ILogger _logger;
        private readonly TseData _fallbackTseData;

        public DailyClosingSplitter(
            IReadOnlyReceiptJournalRepository receiptJournalRepository,
            IReadOnlyQueueItemRepository queueItemRepository,
            IReadOnlyReceiptReferenceRepository receiptReferenceRepository,
            IDailyClosingRepository dailyClosingRepository,
            ILogger logger,
            TseData fallbackTseData)
        {
            _receiptJournalRepository = receiptJournalRepository;
            _queueItemRepository = queueItemRepository;
            _receiptReferenceRepository = receiptReferenceRepository;
            _dailyClosingRepository = dailyClosingRepository;
            _logger = logger;
            _fallbackTseData = fallbackTseData;
        }

        public async IAsyncEnumerable<(IList<ReceiptEntry> dailyEntries, List<ReceiptReferencesGroupedData> references, int zNumber, TseData tseData)> SplitByDailyClosingReceiptAsync(int firstZNumber, long from, long to, bool skipDuplicatedReceipts, long splitClosingReceiptNumberFrom = 0)
        {
            var receiptJournals = (await _receiptJournalRepository.GetAsync())
                .OrderBy(rj => rj.ftReceiptNumber);

            var dailyClosings = await _dailyClosingRepository.GetAsync();

            var references = await _receiptReferenceRepository.GetReceiptReferenceAsync(from, to, dailyClosings);

            List<ReceiptEntry> currentEntries = new();
            var currentZNumberIncrement = 0;
            TseData tseData = null;

            // Iterate a second time to build the actual response
            foreach (var receiptJournalItem in receiptJournals)
            {
                var queueItem = await _queueItemRepository.GetAsync(receiptJournalItem.ftQueueItemId);
                if (queueItem == null)
                {
                    // A receipt whose queue item was not downloaded must fail the export, not silently
                    // disappear from the output - every receipt journal entry belongs in the export, and the
                    // error-state/deserialization skips below are the only intentional exclusions. Upstream
                    // guards (the downloader's retry-then-fail, the DFKA completeness validation) should make
                    // this unreachable; reaching it anyway means the downloaded set itself is inconsistent.
                    _logger.LogError("Receipt {receiptNumber} ({receiptJournalId}) references queue item {queueItemId}, which is not in the downloaded set. (Queue: {queueId})",
                        receiptJournalItem.ftReceiptNumber, receiptJournalItem.ftReceiptJournalId, receiptJournalItem.ftQueueItemId, receiptJournalItem.ftQueueId);
                    throw new InvalidOperationException($"Receipt {receiptJournalItem.ftReceiptNumber} references queue item {receiptJournalItem.ftQueueItemId}, which is not in the downloaded set. Refusing to produce an export that is missing receipts - re-run the export; if this persists, the queue needs investigation.");
                }
                var (request, response) = ExtractRequestAndResponse(queueItem);
                if (request == null || response == null || response.IsErrorState())
                {
                    continue;
                }
                queueItem.request = null;
                queueItem.response = null;

                var duplicateFound = false;
                if (skipDuplicatedReceipts)
                {
                    foreach (var entry in currentEntries)
                    {
                        if (AreReceiptRequestsEqual(entry.ReceiptRequest, request))
                        {
                            duplicateFound = true;
                            break;
                        }
                    }
                }

                if (!duplicateFound && (splitClosingReceiptNumberFrom == 0 || splitClosingReceiptNumberFrom >= receiptJournalItem.ftReceiptNumber))
                {
                    currentEntries.Add(new ReceiptEntry
                    {
                        ReceiptJournal = receiptJournalItem,
                        QueueItem = queueItem,
                        ReceiptRequest = request,
                        ReceiptResponse = response,
                    });
                }

                if (request.IsDailyClosingReceipt())
                {
                    tseData ??= GetTseDataFromDaily(response);
                    var currentZNumber = firstZNumber + currentZNumberIncrement;

                    // TODO: We had "targetZNumber" here previously, but isn't that incorrect? Shouldn't we just return the references that have the current day as the source?
                    // Also, why do we need to order this by the TargetReceiptIdentification? String ordering in my opinion doesn't make sense for hex numbers in the format of ftA1#12345..
                    var currentReferences = references.Where(x => currentZNumber == x.TargetZNumber).OrderBy(x => x.TargetReceiptIdentification).ToList();

                    yield return (currentEntries, currentReferences, currentZNumber, tseData ?? _fallbackTseData);

                    currentEntries = new();
                    currentZNumberIncrement++;
                }
            }
        }

        private (ReceiptRequest request, ReceiptResponse response) ExtractRequestAndResponse(ftQueueItem queueItem)
        {
            if (queueItem == null)
            {
                return (null, null);
            }

            ReceiptRequest request = null;
            ReceiptResponse response = null;

            try
            {
                request = JsonConvert.DeserializeObject<ReceiptRequest>(Eliminater0x0.Eliminate0x0IfNeeded(queueItem.ftQueueId, queueItem.request));
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Request with cbReceiptReference {queueItem.cbReceiptReference} could not be parsed. \n Request: \n {queueItem.request} \n Error: {ex.Message} ");
            }

            try
            {
                if (queueItem.response != null)
                {
                    response = JsonConvert.DeserializeObject<ReceiptResponse>(Eliminater0x0.Eliminate0x0IfNeeded(queueItem.ftQueueId, queueItem.response));
                    response = RemoveUnusedSignatures(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Response with cbReceiptReference {queueItem.cbReceiptReference} could not be parsed. \n Request: \n {queueItem.response} \n Error: {ex.Message} ");
            }

            return (request, response);
        }

        private TseData GetTseDataFromDaily(ReceiptResponse dailyClosingResponse)
        {
            return dailyClosingResponse.ftSignatures.Where(x => x.ftSignatureType == 0x444500000000001E).Any()
                ? new TseData
                {
                    PublicKeyBase64 = dailyClosingResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == 0x444500000000001E)?.Data,
                    SignAlgorithm = dailyClosingResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == 0x444500000000001B)?.Data,
                    TimeFormat = dailyClosingResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == 0x444500000000001C)?.Data
                }
                : null;
        }

        private ReceiptResponse RemoveUnusedSignatures(ReceiptResponse receiptResponse)
        {
            if (receiptResponse.ftSignatures == null)
            {
                return receiptResponse;
            }
            var signatures = receiptResponse.ftSignatures.ToList();
            var usedTypes = new List<long>()
            {
                0x4445000000000014, //CashBoxIdentification
                0x444500000000001E, //PublicKey
                0x444500000000001B, //SignaturAlgorithm
                0x444500000000001C, //LogTimeFormat
                0x4445000000000015, //ProcessType
                0x444500000000001A, //LogTime
                0x444500000000001F, //VorgangsBeginn
                0x4445000000000017, //TransactionNumber
                0x4445000000000019, //StartTime
                0x4445000000000018, //SignatureCounter
                0x444500000000001D, //Signature
                0x4445000000000016, //ProcessData
            };
            receiptResponse.ftSignatures = receiptResponse.ftSignatures.ToList().Where(x => usedTypes.Contains(x.ftSignatureType)).ToArray();
            return receiptResponse;
        }

        private bool AreReceiptRequestsEqual(ReceiptRequest request1, ReceiptRequest request2)
        {
            if (request1.cbReceiptReference != request2.cbReceiptReference || request1.cbTerminalID != request2.cbTerminalID)
            {
                return false;
            }

            if (request2.cbChargeItems.Length == request1.cbChargeItems.Length && request2.cbPayItems.Length == request1.cbPayItems.Length)
            {
                for (var i = 0; i < request2.cbChargeItems.Length; i++)
                {
                    if (request2.cbChargeItems[i].Amount != request1.cbChargeItems[i].Amount)
                    {
                        return false;
                    }
                    if (request2.cbChargeItems[i].ftChargeItemCase != request1.cbChargeItems[i].ftChargeItemCase)
                    {
                        return false;
                    }
                    if (request2.cbChargeItems[i].Moment != request1.cbChargeItems[i].Moment)
                    {
                        return false;
                    }
                }
                for (var i = 0; i < request2.cbPayItems.Length; i++)
                {
                    if (request2.cbPayItems[i].Amount != request1.cbPayItems[i].Amount)
                    {
                        return false;
                    }
                    if (request2.cbPayItems[i].ftPayItemCase != request1.cbPayItems[i].ftPayItemCase)
                    {
                        return false;
                    }
                    if (request2.cbPayItems[i].Moment != request1.cbPayItems[i].Moment)
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }
    }
}
