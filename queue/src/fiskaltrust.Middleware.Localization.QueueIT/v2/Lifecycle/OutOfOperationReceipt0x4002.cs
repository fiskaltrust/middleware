using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.Lifecycle
{
    public class OutOfOperationReceipt0x4002 : IReceiptTypeProcessor
    {
        private readonly IITSSCD _itSSCD;
        private readonly IConfigurationRepository _configurationRepository;

        public ITReceiptCases ReceiptCase => ITReceiptCases.OutOfOperationReceipt0x4002;

        public OutOfOperationReceipt0x4002(IITSSCD itSSCD, IConfigurationRepository configurationRepository)
        {
            _itSSCD = itSSCD;
            _configurationRepository = configurationRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            if (queue.IsDeactivated())
            {
                return (receiptResponse, new List<ftActionJournal> { ActionJournalFactory.CreateAlreadyOutOfOperationActionJournal(queue, queueItem, request) });
            }

            var result = await _itSSCD.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = receiptResponse,
            });
            queue.StopMoment = DateTime.UtcNow;
            await _configurationRepository.InsertOrUpdateQueueAsync(queue);

            var signatureItem = SignaturItemFactory.CreateOutOfOperationSignature(queueIt);
            var actionJournal = ActionJournalFactory.CreateOutOfOperationActionJournal(queue, queueItem, queueIt, request);
            var signatures = new List<SignaturItem>
                {
                    signatureItem
                };
            signatures.AddRange(result.ReceiptResponse.ftSignatures);
            receiptResponse.ftSignatures = signatures.ToArray();
            return (receiptResponse, new List<ftActionJournal>
                {
                    actionJournal
                });
        }
    }
}
