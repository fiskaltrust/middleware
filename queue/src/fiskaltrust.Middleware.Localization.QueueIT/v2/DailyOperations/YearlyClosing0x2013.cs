using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using System.Threading.Tasks;
using System.Collections.Generic;
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
            var actionJournalEntry = ActionJournalFactory.CreateDailyClosingActionJournal(queue, queueItem, request);
            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = receiptResponse
            });
            var zNumber = long.Parse(result.ReceiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTZNumber)).Data);
            receiptResponse.ftReceiptIdentification += $"Z{zNumber}";
            return await Task.FromResult((receiptResponse, new List<ftActionJournal>
            {
                actionJournalEntry
            })).ConfigureAwait(false);
        }
    }
}
