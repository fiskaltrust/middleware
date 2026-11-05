using System;
using fiskaltrust.storage.V0;
using Newtonsoft.Json.Linq;

namespace fiskaltrust.Middleware.Journal.DE.Common.Models
{
    public class ReceiptReferenceData
    {
        public ReceiptReferenceData() { }

        public ReceiptReferenceData(ftQueueItem queueItem)
        {
            SourceZMoment = queueItem.cbReceiptMoment;
            SourceReceiptIdentification = JObject.Parse(queueItem.response)["ftReceiptIdentification"]?.ToString();
        }

        public DateTime? SourceZMoment { get; set; }
        public string SourceReceiptIdentification { get; set; }
        public long? SourceZNumber { get; set; }
        public Guid? SourceQueueItemId { get; set; }
        public Guid? TargetQueueItemId { get; set; }
    }
}
