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
    public class InitialOperationReceiptCommand : RequestCommand
    {
        public InitialOperationReceiptCommand(ILogger<RequestCommand> logger, IConfigurationRepository configurationRepository,
            IMiddlewareJournalMERepository journalMERepository, IMiddlewareQueueItemRepository queueItemRepository, IMiddlewareActionJournalRepository actionJournalRepository, QueueMEConfiguration queueMeConfiguration) :
            base(logger, configurationRepository, journalMERepository, queueItemRepository, actionJournalRepository, queueMeConfiguration)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueME queueME)
        {
            try
            {
                var enu = JsonConvert.DeserializeObject<Tcr>(request.ftReceiptCaseData);
                if (await EnuExists(queueME, enu).ConfigureAwait(false))
                {
                    throw new EnuAlreadyRegisteredException();
                };
                var registerTCRRequest = new RegisterTcrRequest()
                {
                    BusinessUnitCode = enu.BusinessUnitCode,
                    TcrSoftwareCode = enu.SoftwareCode,
                    TcrSoftwareMaintainerCode = enu.MaintainerCode,
                    InternalTcrIdentifier = queueItem.ftQueueItemId.ToString(),
                    RequestId = Guid.NewGuid(),

                };
                var tcrType = TcrType.Regular;
                if (!string.IsNullOrEmpty(enu.TcrType) && Enum.TryParse(enu.TcrType, out tcrType))
                {
                    registerTCRRequest.TcrType = tcrType;
                }
                var registerTCRResponse = await client.RegisterTcrAsync(registerTCRRequest).ConfigureAwait(false);
                var signaturCreationUnitME = new ftSignaturCreationUnitME()
                {
                    ftSignaturCreationUnitMEId = Guid.NewGuid(),
                    TimeStamp = DateTime.UtcNow.Ticks,
                    TcrIntId = enu.TcrIntId,
                    BusinessUnitCode = enu.BusinessUnitCode,
                    IssuerTin = enu.IssuerTin,
                    SoftwareCode = enu.SoftwareCode,
                    MaintainerCode = enu.MaintainerCode,
                    TcrCode = registerTCRResponse.TcrCode,
                    ValidFrom = enu.ValidFrom ?? DateTime.UtcNow
                };
                await ConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(signaturCreationUnitME).ConfigureAwait(false);
                queueME.ftSignaturCreationUnitMEId = signaturCreationUnitME.ftSignaturCreationUnitMEId;
                await ConfigurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
                var receiptResponse = CreateReceiptResponse(request, queueItem);
                return new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse
                };
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "An exception occured while processing this request.");
                throw;
            }
        }

        private async Task<bool> EnuExists(ftQueueME queueME, Tcr enu)
        {
            if (queueME != null && queueME.ftSignaturCreationUnitMEId.HasValue)
            {
                var scuME = await ConfigurationRepository.GetSignaturCreationUnitMEAsync(queueME.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
                if(scuME == null)
                {
                    return false;
                }
                if (scuME.IssuerTin != null && scuME.IssuerTin.Equals(enu.IssuerTin) && scuME.TcrIntId != null && scuME.TcrIntId.Equals(enu.TcrIntId))
                {
                    throw new EnuAlreadyRegisteredException();
                }
                if (scuME.IssuerTin != null && !scuME.IssuerTin.Equals(enu.IssuerTin) && scuME.TcrIntId != null && !scuME.TcrIntId.Equals(enu.TcrIntId))
                {
                    throw new EnuAlreadyRegisteredException($"Another Enu for Queue was already registered with IssuerTin: {scuME.IssuerTin} TcrIntId {scuME.TcrIntId}");
                }
            }
            return false;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueueME queueME, ftQueueItem queueItem, ReceiptRequest request) => Task.FromResult(false);
    }
}
