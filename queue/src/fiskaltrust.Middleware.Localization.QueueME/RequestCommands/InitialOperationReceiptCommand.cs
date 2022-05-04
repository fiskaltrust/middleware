using System;
using System.Threading.Tasks;

using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class InitialOperationReceiptCommand : RequestCommand
    {
        public InitialOperationReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository, IJournalMERepository journalMERepository) : base(logger, signatureFactory, configurationRepository, journalMERepository)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            try
            {
                //Validate must fields
                var enu = JsonConvert.DeserializeObject<TCR>(request.ftReceiptCaseData);
                var queueME = await _configurationRepository.GetQueueMEAsync(queue.ftQueueId).ConfigureAwait(false);
                if (queueME != null && queueME.ftSignaturCreationUnitMEId.HasValue)
                {
                    var scuME = await _configurationRepository.GetSignaturCreationUnitMEAsync(queueME.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);
                    if(scuME.IssuerTin != null && scuME.IssuerTin.Equals(enu.IssuerTIN) && scuME.TcrIntId != null && scuME.TcrIntId.Equals(enu.TCRIntID))
                    {
                        throw new ENUAlreadyRegisteredException();
                    }
                }
                var registerTCRRequest = new RegisterTcrRequest()
                {
                    BusinessUnitCode = enu.BusinUnitCode,
                    TcrSoftwareCode = enu.SoftwareCode,
                    TcrSoftwareMaintainerCode = enu.SoftwareCode,
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
                    TimeStamp = DateTime.Now.Ticks,
                    TcrIntId = enu.TCRIntID,
                    BusinessUnitCode = enu.BusinUnitCode,
                    IssuerTin = enu.IssuerTIN,
                    SoftwareCode = enu.SoftwareCode,
                    TcrCode = registerTCRResponse.TcrCode,                    
                    ValidFrom = enu.ValidFrom,
                    ValidTo = enu.ValidTo
                };
                await _configurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(signaturCreationUnitME).ConfigureAwait(false);
                queueME = new ftQueueME()
                {
                    ftQueueMEId = queue.ftQueueId,
                    ftSignaturCreationUnitMEId = signaturCreationUnitME.ftSignaturCreationUnitMEId
                };
                await _configurationRepository.InsertOrUpdateQueueMEAsync(queueME).ConfigureAwait(false);
                var receiptResponse = CreateReceiptResponse(request, queueItem);
                return new RequestCommandResponse()
                {
                    ReceiptResponse = receiptResponse
                };
            }
            catch (Exception ex) when (ex.GetType().Name == RETRYPOLICYEXCEPTION_NAME)
            {
                _logger.LogDebug(ex, "TSE not reachable.");
                throw;
            }

        }
    }
}
