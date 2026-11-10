using System;

namespace fiskaltrust.Middleware.Journal.DE.Common.Models
{
    public class ReceiptReferencesGroupedData : ReceiptReferenceData
    {
        public string TargetReceiptCaseData { get; set; }
        public string TargetReceiptIdentification { get; set; }
        public long? TargetZNumber { get; set; }
        public DateTime? TargetZMoment { get; set; }
        public string TargetReceiptReference { get; set; }
        public string SourceReceiptReference { get; set; }
        public long TargetQueueRow { get; set; }
        public long SourceQueueRow { get; set; }
    }
}
