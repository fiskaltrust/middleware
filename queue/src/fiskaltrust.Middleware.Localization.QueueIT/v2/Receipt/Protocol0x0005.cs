using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.Receipt
{
    public class Protocol0x0005 : IReceiptTypeProcessor
    {
        private readonly IITSSCD _itSSCD;
        private readonly IJournalITRepository _journalITRepository;

        public ITReceiptCases ReceiptCase => ITReceiptCases.Protocol0x0005;

        public Protocol0x0005(IITSSCD itSSCD, IJournalITRepository journalITRepository)
        {
            _itSSCD = itSSCD;
            _journalITRepository = journalITRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var result = await _itSSCD.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = receiptResponse,
            });
            var documentNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber);
            var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber);
            receiptResponse.ftReceiptIdentification += $"{zNumber.Data.PadLeft(4, '0')}-{documentNumber.Data.PadLeft(4, '0')}";
            receiptResponse.ftSignatures = result.ReceiptResponse.ftSignatures;
            receiptResponse.InsertSignatureItems(SignaturBuilder.CreatePOSReceiptFormatSignatures(receiptResponse));
            var journalIT = ftJournalITFactory.CreateFrom(queueItem, queueIt, new ScuResponse()
            {
                ftReceiptCase = request.ftReceiptCase,
                ReceiptNumber = long.Parse(documentNumber.Data),
                ZRepNumber = long.Parse(zNumber.Data)
            });
            await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
            return (receiptResponse, new List<ftActionJournal>());
        }
    }
}
