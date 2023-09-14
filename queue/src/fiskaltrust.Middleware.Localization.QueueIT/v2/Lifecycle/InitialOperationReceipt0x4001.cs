using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.Lifecycle
{
    public class InitialOperationReceipt0x4001 : IReceiptTypeProcessor
    {
        private readonly IITSSCD _itSSCD;
        private readonly IConfigurationRepository _configurationRepository;

        public ITReceiptCases ReceiptCase => ITReceiptCases.InitialOperationReceipt0x4001;

        public InitialOperationReceipt0x4001(IITSSCD itSSCD, IConfigurationRepository configurationRepository)
        {
            _itSSCD = itSSCD;
            _configurationRepository = configurationRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            if (queue.IsNew())
            {
                var scu = await _configurationRepository.GetSignaturCreationUnitITAsync(queueIt.ftSignaturCreationUnitITId.Value).ConfigureAwait(false);
                var deviceInfo = await _itSSCD.GetRTInfoAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(scu.InfoJson))
                {
                    scu.InfoJson = JsonConvert.SerializeObject(deviceInfo);
                    await _configurationRepository.InsertOrUpdateSignaturCreationUnitITAsync(scu).ConfigureAwait(false);
                }

                var signature = SignaturItemFactory.CreateInitialOperationSignature(queueIt, deviceInfo);
                var actionJournal = ActionJournalFactory.CreateInitialOperationActionJournal(queue, queueItem, queueIt, request);
                queue.StartMoment = DateTime.UtcNow;

                await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
                var result = await _itSSCD.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request,
                    ReceiptResponse = receiptResponse,
                });

                var signatures = new List<SignaturItem>
                {
                    signature
                };
                signatures.AddRange(result.ReceiptResponse.ftSignatures);
                receiptResponse.ftSignatures = signatures.ToArray();

                return (receiptResponse, new List<ftActionJournal>
                {
                    actionJournal
                });
            }
            else
            {
                return (receiptResponse, new List<ftActionJournal>
                {
                    ActionJournalFactory.CreateWrongStateForInitialOperationActionJournal(queue, queueItem, request)
                });
            }
        }
    }
}
