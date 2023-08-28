using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.ifPOS.v1.it;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.DailyOperations
{
    public class YearlyClosing0x2013 : IReceiptTypeProcessor
    {
        private readonly IITSSCDProvider _itSSCDProvider;

        public ITReceiptCases ReceiptCase => ITReceiptCases.YearlyClosing0x2013;

        public bool FailureModeAllowed => true;

        public bool GenerateJournalIT => true;

        public YearlyClosing0x2013(IITSSCDProvider itSSCDProvider)
        {
            _itSSCDProvider = itSSCDProvider;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var ftReceiptCaseHex = request.ftReceiptCase.ToString("X");
            var actionJournalEntry = CreateActionJournal(queue.ftQueueId, ftReceiptCaseHex, queueItem.ftQueueItemId, $"Yearly-Closing receipt was processed.", JsonConvert.SerializeObject(new { ftReceiptNumerator = queue.ftReceiptNumerator + 1 }));

            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = receiptResponse
            });
            var zNumber = long.Parse(result.ReceiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ZNumber)).Data);
            receiptResponse.ftReceiptIdentification += $"Z{zNumber}";
            return await Task.FromResult((receiptResponse, new List<ftActionJournal>
            {
                actionJournalEntry
            })).ConfigureAwait(false);
        }

        protected ftActionJournal CreateActionJournal(Guid queueId, string type, Guid queueItemId, string message, string data, int priority = -1)
        {
            return new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queueId,
                ftQueueItemId = queueItemId,
                Type = type,
                Moment = DateTime.UtcNow,
                Message = message,
                Priority = priority,
                DataJson = data
            };
        }
    }
}
