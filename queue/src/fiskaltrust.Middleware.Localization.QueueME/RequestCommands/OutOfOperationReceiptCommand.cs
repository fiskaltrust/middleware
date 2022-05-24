using System;
using System.Threading.Tasks;

using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class OutOfOperationReceiptCommand : RequestCommand
    {
        public OutOfOperationReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMERepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository) :
            base(logger, configurationRepository, journalMERepository, queueItemRepository, actionJournalRepository)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME)
        {
            try
            {
                var enu = JsonConvert.DeserializeObject<Tcr>(request.ftReceiptCaseData);
                if(queueME == null || !queueME.ftSignaturCreationUnitMEId.HasValue)
                {
                    throw new ENUNotRegisteredException();
                }
                var scuME = await _configurationRepository.GetSignaturCreationUnitMEAsync(queueME.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
                if (!enu.IssuerTin.Equals(scuME.IssuerTin) || !enu.TcrIntId.Equals(scuME.TcrIntId))
                {
                    var errormessage = $"Request TCRIntID {enu.TcrIntId} and IssuerTIN {enu.IssuerTin} don´t match Queue initialisation with {enu.TcrIntId} and IssuerTIN {scuME.IssuerTin}";
                    throw new ENUIIDDontMatchException(errormessage);
                }
                var unregisterTCRRequest = new UnregisterTcrRequest()
                {
                    RequestId = queueItem.ftQueueItemId,
                    BusinessUnitCode = scuME.BusinessUnitCode,
                    InternalTcrIdentifier = scuME.TcrIntId,
                };
                await client.UnregisterTcrAsync(unregisterTCRRequest).ConfigureAwait(false);
                scuME.ValidTo = DateTime.UtcNow;
                await _configurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
                var receiptResponse = CreateReceiptResponse(request, queueItem);
                return new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse
                };
            }
            catch (Exception ex) when (ex.GetType().Name == ENDPOINTNOTFOUND)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                throw;
            }
        }
        public override Task<bool> ReceiptNeedsReprocessing(ftQueueME queueME, ftQueueItem queueItem, ReceiptRequest request) => Task.FromResult(false);
    }
}
