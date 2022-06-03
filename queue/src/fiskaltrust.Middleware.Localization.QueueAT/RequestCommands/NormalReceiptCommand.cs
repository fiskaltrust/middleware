using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueAT.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueAT.RequestCommands
{
    internal class NormalReceiptCommand : RequestCommand
    {
        public override string ReceiptName => throw new NotImplementedException();

        public override Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ftQueueAT queueAT, ReceiptRequest request, ftQueueItem queueItem) 
            => throw new NotImplementedException();
    }
}
