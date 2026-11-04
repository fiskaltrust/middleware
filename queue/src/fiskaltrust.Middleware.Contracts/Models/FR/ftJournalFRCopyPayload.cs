using System;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Contracts.Models.FR
{
    public class ftJournalFRCopyPayload
    {
        [JsonProperty("qid")]
        public Guid QueueId { get; set; }
        [JsonProperty("cbid")]
        public string CashBoxIdentification { get; set; }
        [JsonProperty("siret")]
        public string Siret { get; set; }
        [JsonProperty("rid")]
        public string ReceiptId { get; set; }
        [JsonProperty("dt")]
        public DateTime ReceiptMoment { get; set; }
        [JsonProperty("qiid")]
        public Guid QueueItemId { get; set; }
        [JsonProperty("crr")]
        public string CopiedReceiptReference { get; set; }
        [JsonProperty("csn")]
        public string CertificateSerialNumber { get; set; }
        public long TimeStamp { get; set; }

        /// <summary>
        /// The queue row of the copy's queue item, written by QueueFR v2 (market-fr#235) so the copy's
        /// outcome can be read without a scan in any storage. Null on a row written by v1.2.
        /// </summary>
        [JsonProperty("qr")]
        public long? QueueRow { get; set; }
    }
}