using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueME.Extensions;
using fiskaltrust.Middleware.Localization.QueueME.Factories;
using fiskaltrust.storage.serialization.DE.V0;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class OutOfOperationReceiptCommand : RequestCommand
    {
        private readonly SignatureItemFactory _signatureItemFactory;

        public OutOfOperationReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMERepository, IMiddlewareQueueItemRepository queueItemRepository,
            IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration, SignatureItemFactory signatureItemFactory) :
            base(logger, configurationRepository, journalMERepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        {
            _signatureItemFactory = signatureItemFactory;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME, bool subsequent = false)
        {
            try
            {
                var scuME = await ConfigurationRepository.GetSignaturCreationUnitMEAsync(queueME.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
                var unregisterTCRRequest = new UnregisterTcrRequest()
                {
                    RequestId = queueItem.ftQueueItemId,
                    BusinessUnitCode = scuME.BusinessUnitCode,
                    InternalTcrIdentifier = scuME.TcrIntId,
                    TcrSoftwareCode = scuME.SoftwareCode,
                    TcrSoftwareMaintainerCode = scuME.MaintainerCode,
                };
                await client.UnregisterTcrAsync(unregisterTCRRequest).ConfigureAwait(false);
               
                scuME.ValidTo = DateTime.UtcNow;
                await ConfigurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
                
                var receiptResponse = CreateReceiptResponse(queue, request, queueItem);
                receiptResponse.ftSignatures = receiptResponse.ftSignatures.Extend(_signatureItemFactory.CreateInitialOperationSignature(queue.ftQueueId, scuME.TcrCode));

                var notification = new DeactivateQueueSCU
                {
                    CashBoxId = Guid.Parse(request.ftCashBoxID),
                    QueueId = queueItem.ftQueueId,
                    Moment = DateTime.UtcNow,
                    SCUId = queueME.ftSignaturCreationUnitMEId.GetValueOrDefault(),
                    IsStopReceipt = true,
                    Version = "V0"
                };
                var actionJournal = new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueItem.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    Priority = -1,
                    TimeStamp = 0,
                    Message = $"Out-of-Operation receipt. TCR-Code: {scuME.TcrCode}, Queue-ID: {queue.ftQueueId}",
                    Type = $"{0x4D45000000000004:X}-{nameof(DeactivateQueueSCU)}",
                    DataJson = JsonConvert.SerializeObject(notification)
                };

                queue.StopMoment = DateTime.UtcNow;
                return new RequestCommandResponse
                {
                    ReceiptResponse = receiptResponse,
                    ActionJournals = new List<ftActionJournal> { actionJournal }
                };
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "An exception occured while processing this request.");
                throw;
            }
        }
        public override Task<bool> ReceiptNeedsReprocessing(ftQueueME queueME, ftQueueItem queueItem, ReceiptRequest request) => Task.FromResult(false);
    }
}
