using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.QueueME.Models;

namespace fiskaltrust.Middleware.Localization.QueueME.RequestCommands
{
    public abstract class RequestCommand
    {

        protected readonly ILogger<RequestCommand> _logger;
        protected readonly SignatureFactoryME _signatureFactory;

        public RequestCommand(ILogger<RequestCommand> logger, SignatureFactoryME signatureFactory)
        {
            _logger = logger;
            _signatureFactory = signatureFactory;
        }

        public abstract Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem);

    }
}