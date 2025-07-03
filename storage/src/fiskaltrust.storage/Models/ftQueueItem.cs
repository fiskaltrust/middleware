using System;

namespace fiskaltrust.storage.V0
{
    public class ftQueueItem
    {
        /// <summary>
        /// PK
        /// </summary>
        public Guid ftQueueItemId { get; set; }

        /// <summary>
        /// Reference to ftQueue, part of unique index UNIQUE_INDEX_ftQueueItem_ftQueueId_ftQueueRow
        /// </summary>
        public Guid ftQueueId { get; set; }

        /// <summary>
        /// Rownumber, part of unique index UNIQUE_INDEX_ftQueueItem_ftQueueId_ftQueueRow
        /// </summary>
        public long ftQueueRow { get; set; }

        public DateTime ftQueueMoment { get; set; }

        /// <summary>
        /// Timeout of the queued Request in ms
        /// </summary>
        public int ftQueueTimeout { get; set; }

        public DateTime? ftWorkMoment { get; set; }
        public DateTime? ftDoneMoment { get; set; }

        public DateTime cbReceiptMoment { get; set; }
        public string cbTerminalID { get; set; }
        public string cbReceiptReference { get; set; }

        public string country { get; set; }
        public string version { get; set; }
        public string request { get; set; }
        public string requestHash { get; set; }
        public string response { get; set; }
        public string responseHash { get; set; }

        public long TimeStamp { get; set; }

        /// <summary>
        /// Processing version of the queue item
        /// </summary>
        public string ProcessingVersion { get; set; }
    }
}