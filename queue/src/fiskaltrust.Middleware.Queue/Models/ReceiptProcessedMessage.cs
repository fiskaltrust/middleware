using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Queue.Models
{
    public class ReceiptProcessedMessage
    {
        public ReceiptRequest Request { get; set; }
        public ReceiptResponse Response { get; set; }
        public ftReceiptJournal ReceiptJournal { get; set; }
    }
}
