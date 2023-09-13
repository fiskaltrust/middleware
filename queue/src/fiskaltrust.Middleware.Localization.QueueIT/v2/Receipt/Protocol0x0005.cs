using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.ifPOS.v1.it;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.Receipt
{
    public class Protocol0x0005 : IReceiptTypeProcessor
    {
        private readonly IITSSCDProvider _itSSCDProvider;

        public ITReceiptCases ReceiptCase => ITReceiptCases.Protocol0x0005;

        public Protocol0x0005(IITSSCDProvider itSSCDProvider)
        {
            _itSSCDProvider = itSSCDProvider;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var result = await _itSSCDProvider.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = receiptResponse,
            });
            var documentNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber);
            var zNumber = result.ReceiptResponse.GetSignaturItem(SignatureTypesIT.RTZNumber);
            receiptResponse.ftReceiptIdentification += $"{zNumber.Data.PadLeft(4, '0')}-{documentNumber.Data.PadLeft(4, '0')}";
            receiptResponse.ftSignatures = result.ReceiptResponse.ftSignatures;
            return (receiptResponse, new List<ftActionJournal>());
        }
    }
}
