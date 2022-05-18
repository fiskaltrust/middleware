using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Queue.Models
{
    public class ReceiptProcessedMessage
    {
        public ftQueueItem QueueItem { get; set; }
        public ftReceiptJournal ReceiptJournal { get; set; }
    }
}
