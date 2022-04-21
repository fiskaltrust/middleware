using System;
using System.Threading.Tasks;

using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2.me;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class OutOfOperationReceiptCommand : RequestCommand
    {
        public OutOfOperationReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository, IMasterDataRepository<OutletMasterData> outletMasterDataRepository) : base(logger, signatureFactory, configurationRepository, outletMasterDataRepository)
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

                if (!enu.IssuerTIN.Trim().Equals(queueME.IssuerTIN.Trim()) || !enu.TCRIntID.Trim().Equals(queueME.TCRIntID.Trim()))
                {
                    var errormessage = $"Request TCRIntID {enu.TCRIntID} and IssuerTIN {enu.IssuerTIN} don´t match Queue initialisation with {queueME.TCRIntID} and IssuerTIN {queueME.IssuerTIN}";
                    throw new ENUIIDDontMatchException(errormessage);
                }

                if (!enu.ValidTo.HasValue)
                {
                    throw new ENUValidToNotSetException();
                }

                var tcr = new TCRType()
                {
                    TCRIntID = enu.TCRIntID,
                    IssuerTIN = enu.IssuerTIN,
                    BusinUnitCode = enu.BusinUnitCode,
                    ValidFrom = queueME.ValidFrom,
                    ValidTo = enu.ValidTo,
                };

                var registerTCRRequest = new RegisterTCRRequest()
                {
                    TCR = tcr,
                    Signature = _signatureFactory.CreateSignature()
                };
                var registerTCRResponse = await client.RegisterTCRAsync(registerTCRRequest).ConfigureAwait(false);

                queueME.ValidTo = tcr.ValidTo;
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
