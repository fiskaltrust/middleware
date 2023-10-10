using System;

namespace fiskaltrust.Middleware.Localization.QueueIT.Models
{
    public struct ScuResponse
    {
        public long ftReceiptCase { get; set; }
        public DateTime ReceiptDateTime { get; set; }
        public long ReceiptNumber { get; set; }
        public long ZRepNumber { get; set; }
        public string DataJson { get; set; }
    }
}
