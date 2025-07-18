using System;

namespace fiskaltrust.storage.V0
{
    public class ftJournalFRCopyPayload
    {
        public Guid QueueId { get; set; }
        public string CashBoxIdentification { get; set; }
        public string Siret { get; set; }
        public string ReceiptId { get; set; }
        public DateTime ReceiptMoment { get; set; }
        public Guid QueueItemId { get; set; }
        public string CopiedReceiptReference { get; set; }
        public string CertificateSerialNumber { get; set; }
    }
}