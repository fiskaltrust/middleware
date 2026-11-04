using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Journal.DE.Common.Models
{
    public class ReceiptEntry
    {
        public ftQueueItem QueueItem { get; set; }
        public ReceiptRequest ReceiptRequest { get; set; }
        public ReceiptResponse ReceiptResponse { get; set; }
        public ftReceiptJournal ReceiptJournal { get; set; }
    }
}
