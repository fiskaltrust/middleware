﻿using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities
{
    public class AzureFtQueueItem : BaseTableEntity
    {
        public string response { get; set; }
        public string requestHash { get; set; }
        public string request { get; set; }
        public string version { get; set; }
        public string country { get; set; }
        public string cbReceiptReference { get; set; }
        public string cbTerminalID { get; set; }
        public DateTime cbReceiptMoment { get; set; }
        public DateTime? ftDoneMoment { get; set; }
        public DateTime? ftWorkMoment { get; set; }
        public int ftQueueTimeout { get; set; }
        public DateTime ftQueueMoment { get; set; }
        public long ftQueueRow { get; set; }
        public Guid ftQueueId { get; set; }
        public Guid ftQueueItemId { get; set; }
        public string responseHash { get; set; }
        public long TimeStamp { get; set; }
    }
}