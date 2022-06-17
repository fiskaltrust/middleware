using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.Middleware.Localization.QueueAT.Services;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    internal class HandwrittenReceiptCommand : RequestCommand
    {
        public override string ReceiptName => throw new NotImplementedException();

        public HandwrittenReceiptCommand(IATSSCDProvider sscdProvider, MiddlewareConfiguration middlewareConfiguration, QueueATConfiguration queueATConfiguration, ILogger<RequestCommand> logger)
            : base(sscdProvider, middlewareConfiguration, queueATConfiguration, logger) { }

        public override Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request, ftQueueItem queueItem) 
            => throw new NotImplementedException();
    }
}
