using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePT.Interface.Models
{
    public record ProcessCommandRequest(ftQueue Queue, ftQueuePT QueuePt, ReceiptRequest ReceiptRequest, ReceiptResponse ReceiptResponse, ftQueueItem QueueItem);
}
