using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.storage.serialization.DE.V0;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v2.me;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class InitialOperationReceiptCommand : RequestCommand
    {
        public InitialOperationReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository) : base(logger, signatureFactory, configurationRepository)
        { }

        public override async Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            try
            {
                var enu = JsonConvert.DeserializeObject<TCR>(request.ftReceiptCaseData);
                var queueMEs = await _configurationRepository.GetQueueMEListAsync().ConfigureAwait(false);
                var queueME = queueMEs.ToList().Where(x => x.IssuerTIN.Equals(enu.IssuerTIN) && x.TCRIntID.Equals(enu.TCRIntID)).FirstOrDefault();
                if (queueME != null)
                {
                    throw new ENUAlreadyRegisteredException();
                }

                var tcr = new TCRType()
                {
                    TCRIntID = enu.TCRIntID,
                    IssuerTIN = enu.IssuerTIN,
                    BusinUnitCode = enu.BusinUnitCode
                };

                var registerTCRRequest = new RegisterTCRRequest()
                {
                    TCR = tcr,
                    Signature = _signatureFactory.CreateSignature()
                };
                var registerTCRResponse = client.RegisterTCR(registerTCRRequest);

                var signaturCreationUnitME = new ftSignaturCreationUnitME()
                {
                    ftSignaturCreationUnitMEId = Guid.NewGuid(),
                    TimeStamp = DateTime.Now.Ticks,
                };
                await _configurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(signaturCreationUnitME).ConfigureAwait(false);

                queueME = new ftQueueME()
                {
                    ftQueueMEId = queue.ftQueueId,
                    TCRIntID = tcr.TCRIntID,
                    BusinUnitCode = tcr.BusinUnitCode,
                    IssuerTIN = tcr.IssuerTIN,
                    TCRCode = registerTCRResponse.TCRCode,
                    ftSignaturCreationUnitMEId = signaturCreationUnitME.ftSignaturCreationUnitMEId,
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
