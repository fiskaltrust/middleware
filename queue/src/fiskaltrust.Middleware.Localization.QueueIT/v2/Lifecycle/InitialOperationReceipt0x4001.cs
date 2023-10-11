using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Helpers;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2.Lifecycle
{
    public class InitialOperationReceipt0x4001 : IReceiptTypeProcessor
    {
        private readonly IITSSCD _itSSCD;
        private readonly IConfigurationRepository _configurationRepository;

        public ReceiptCases ReceiptCase => ReceiptCases.InitialOperationReceipt0x4001;

        public InitialOperationReceipt0x4001(IITSSCD itSSCD, IConfigurationRepository configurationRepository)
        {
            _itSSCD = itSSCD;
            _configurationRepository = configurationRepository;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ExecuteAsync(ftQueue queue, ftQueueIT queueIt, ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            try
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
                    var actionJournal = ftActionJournalFactory.CreateInitialOperationActionJournal(queue, queueItem, queueIt, request);
                    queue.StartMoment = DateTime.UtcNow;
                    
                    var result = await _itSSCD.ProcessReceiptAsync(new ProcessRequest
                    {
                        ReceiptRequest = request,
                        ReceiptResponse = receiptResponse,
                    });
                    await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
                    

                    if (result.ReceiptResponse.HasFailed())
                    {
                        return (result.ReceiptResponse, new List<ftActionJournal>
                        {
                            actionJournal
                        });
                    }

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
                        ftActionJournalFactory.CreateWrongStateForInitialOperationActionJournal(queue, queueItem, request)
                    });
                }
            }
            catch (Exception ex)
            {
                var signatures = new List<SignaturItem>
                {
                    new SignaturItem
                    {
                        Caption = "queue-initialoperation-generic-error",
                        Data = $"{ex}",
                        ftSignatureFormat = (long) SignaturItem.Formats.Text,
                        ftSignatureType = 0x4954_2000_0000_3000
                    }
                };
                receiptResponse.ftSignatures = signatures.ToArray();
                receiptResponse.ftState = 0x4954_2001_EEEE_EEEE;
                return (receiptResponse, new List<ftActionJournal>());
            }
        }
    }
}
