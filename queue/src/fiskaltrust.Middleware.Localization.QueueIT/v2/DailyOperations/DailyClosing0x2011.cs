using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.DailyOperations
{
    public class DailyClosing0x2011 : IReceiptTypeProcessor
    {
        private readonly IITSSCD _itSSCD;
        private readonly IJournalITRepository _journalITRepository;

        public ITReceiptCases ReceiptCase => ITReceiptCases.DailyClosing0x2011;

        public DailyClosing0x2011(IITSSCD itSSCD, IJournalITRepository journalITRepository)
        {
            _itSSCD = itSSCD;
            _journalITRepository = journalITRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            try
            {
                var actionJournalEntry = ActionJournalFactory.CreateDailyClosingActionJournal(queue, queueItem, request);
                var result = await _itSSCD.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request,
                    ReceiptResponse = receiptResponse
                });
                var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber).Data;
                receiptResponse.ftReceiptIdentification += $"Z{zNumber.PadLeft(4, '0')}";
                receiptResponse.ftSignatures = result.ReceiptResponse.ftSignatures;

                var journalIT = ftJournalITFactory.CreateFrom(queueItem, queueIt, new ScuResponse()
                {
                    ftReceiptCase = request.ftReceiptCase,
                    ZRepNumber = long.Parse(zNumber)
                });
                await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);

                return (receiptResponse, new List<ftActionJournal>
                {
                    actionJournalEntry
                });
            }
            catch (Exception ex)
            {
                receiptResponse.SetReceiptResponseErrored($"The daily closing operation failed with the following error message: {ex.Message}");
                return (receiptResponse, new List<ftActionJournal>());
            }
        }
    }
}
