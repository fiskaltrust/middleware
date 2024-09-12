using System.Runtime.CompilerServices;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2.v2
{
    public record ProcessCommandRequest(ftQueue Queue, ftQueuePT QueuePt, ReceiptRequest ReceiptRequest, ReceiptResponse ReceiptResponse, ftQueueItem QueueItem);
}
