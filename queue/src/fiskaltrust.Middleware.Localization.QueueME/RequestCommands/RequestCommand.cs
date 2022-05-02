using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using System;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public abstract class RequestCommand
    {
        protected const string RETRYPOLICYEXCEPTION_NAME = "RetryPolicyException";
        protected readonly ILogger<RequestCommand> _logger;
        protected readonly SignatureFactoryME _signatureFactory;
        protected readonly IConfigurationRepository _configurationRepository;
        protected readonly IMasterDataRepository<OutletMasterData> _outletMasterDataRepository;


        public RequestCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory, IConfigurationRepository configurationRepository, IMasterDataRepository<OutletMasterData> outletMasterDataRepository)
        {
            _logger = logger;
            _signatureFactory = signatureFactory;
            _configurationRepository = configurationRepository;
            _outletMasterDataRepository = outletMasterDataRepository;
        }

        public abstract Task<RequestCommandResponse> ExecuteAsync(IMESSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem);

        protected static ReceiptResponse CreateReceiptResponse(ReceiptRequest request, ftQueueItem queueItem)
        {
            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0x44D5000000000000
            };
        }

    }
}