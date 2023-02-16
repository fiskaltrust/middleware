using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1.it;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public abstract class RequestCommandIT : RequestCommand
    {
        public override long CountryBaseState => 0x4954000000000000;

        public abstract Task<RequestCommandResponse> ExecuteAsync(IITSSCD client, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, ftQueueIT queueIt);

    }
}
