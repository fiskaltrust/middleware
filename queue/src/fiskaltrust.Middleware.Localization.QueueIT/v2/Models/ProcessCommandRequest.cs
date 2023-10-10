using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueIT.v2
{
    public record ProcessCommandRequest(ftQueue Queue, ftQueueIT QueueIt, ReceiptRequest ReceiptRequest, ReceiptResponse ReceiptResponse, ftQueueItem QueueItem);
}
