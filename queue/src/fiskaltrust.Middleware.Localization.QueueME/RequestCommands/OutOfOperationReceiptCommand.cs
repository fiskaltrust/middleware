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
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class OutOfOperationReceiptCommand : RequestCommand
    {
        public OutOfOperationReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository, IJournalMERepository journalMERepository) : base(logger, signatureFactory, configurationRepository, journalMERepository)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            try
            {
                var enu = JsonConvert.DeserializeObject<TCR>(request.ftReceiptCaseData);
                var queueME = await _configurationRepository.GetQueueMEAsync(queue.ftQueueId).ConfigureAwait(false);
                if(queueME == null)
                {
                    throw new ENUNotRegisteredException();
                }

                var scuME = await _configurationRepository.GetSignaturCreationUnitMEAsync(queueME.ftSignaturCreationUnitMEId.Value).ConfigureAwait(false);

                if (!enu.IssuerTIN.Equals(scuME.IssuerTin) || !enu.TCRIntID.Equals(scuME.TcrIntId))
                {
                    var errormessage = $"Request TCRIntID {enu.TCRIntID} and IssuerTIN {enu.IssuerTIN} don´t match Queue initialisation with {enu.TCRIntID} and IssuerTIN {scuME.IssuerTin}";
                    throw new ENUIIDDontMatchException(errormessage);
                }

                if (!enu.ValidTo.HasValue)
                {
                    throw new ENUValidToNotSetException();
                }

                var registerTCRRequest = new RegisterTcrRequest()
                {
                    RequestId = queueItem.ftQueueItemId,
                    BusinessUnitCode = scuME.BusinessUnitCode,
                    InternalTcrIdentifier = scuME.TcrIntId               ,
                    TcrSoftwareCode = scuME.SoftwareCode,
                    TcrSoftwareMaintainerCode = scuME.MaintainerCode,
                    TcrType = enu.TcrType == null ? TcrType.Regular : (TcrType) Enum.Parse(typeof(TcrType), enu.TcrType),    
                };
                var registerTCRResponse = await client.RegisterTcrAsync(registerTCRRequest).ConfigureAwait(false);

                scuME.ValidTo = enu.ValidTo;
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
