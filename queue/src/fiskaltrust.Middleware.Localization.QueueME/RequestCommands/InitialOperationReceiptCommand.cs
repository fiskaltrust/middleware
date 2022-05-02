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
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public class InitialOperationReceiptCommand : RequestCommand
    {
        public InitialOperationReceiptCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository, IMasterDataRepository<OutletMasterData> outletMasterDataRepository) : base(logger, signatureFactory, configurationRepository, outletMasterDataRepository)
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

                var registerTCRRequest = new RegisterTcrRequest()
                {
                    TcrType = string.IsNullOrEmpty(enu.tcrType) ? TcrType.Regular : (TcrType) Enum.Parse(typeof(TcrType), enu.tcrType),
                    BusinessUnitCode = enu.BusinUnitCode,
                    TcrSoftwareCode = enu.SoftwareCode,
                    TcrSoftwareMaintainerCode = enu.SoftwareCode,
                    InternalTcrIdentifier = queueItem.ftQueueItemId.ToString(),
                    RequestId = Guid.NewGuid(),

                };
                var registerTCRResponse = await client.RegisterTcrAsync(registerTCRRequest).ConfigureAwait(false);

                var signaturCreationUnitME = new ftSignaturCreationUnitME()
                {
                    ftSignaturCreationUnitMEId = Guid.NewGuid(),
                    TimeStamp = DateTime.Now.Ticks,
                };
                await _configurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(signaturCreationUnitME).ConfigureAwait(false);

                queueME = new ftQueueME()
                {
                    ftQueueMEId = queue.ftQueueId,
                    TCRIntID = enu.TCRIntID,
                    BusinUnitCode = enu.BusinUnitCode,
                    IssuerTIN = enu.IssuerTIN,
                    SoftCode= enu.SoftwareCode,
                    TCRCode = registerTCRResponse.TcrCode,
                    ftSignaturCreationUnitMEId = signaturCreationUnitME.ftSignaturCreationUnitMEId,
                    ValidFrom = enu.ValidFrom,
                    ValidTo = enu.ValidTo
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
