using System;

namespace fiskaltrust.Middleware.Journal.DE.Common.Models
{
    public class ReceiptCaseData
    {
        public string ReceiptName { get; set; }
        public string UserId { get; set; }
        public string ReceiptNote { get; set; }
        public string RefType { get; set; }
        public string RefName { get; set; }
        public DateTime RefMoment { get; set; }
        public string RefCashBoxIdentification { get; set; }
        public long RefClosingNr { get; set; }
        public string RefReceiptId { get; set; }
    }
}
