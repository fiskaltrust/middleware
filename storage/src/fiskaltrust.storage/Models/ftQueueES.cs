using System;

namespace fiskaltrust.storage.V0
{
    public class ftQueueES : QueueLocalization
    {
        public Guid ftQueueId => ftQueueESId;

        public Guid ftSignaturCreationUnitId => ftSignaturCreationUnitESId;

        public Guid ftQueueESId { get; set; }

        public Guid ftSignaturCreationUnitESId { get; set; }

        public string LastHash { get; set; }

        public string CashBoxIdentification { get; set; }

        public Guid? SSCDSignQueueItemId { get; set; }
        
        // Simplified Invoice fields
        public DateTime? LastSimplifiedInvoiceMoment { get; set; }
        public Guid? LastSimplifiedInvoiceQueueItemId { get; set; }
        public long SimplifiedInvoiceNumerator { get; set; }
        public string SimplifiedInvoiceSeries { get; set; }

        // Full Invoice fields
        public DateTime? LastInvoiceMoment { get; set; }
        public Guid? LastInvoiceQueueItemId { get; set; }
        public long InvoiceNumerator { get; set; }
        public string InvoiceSeries { get; set; }

        public int SSCDFailCount { get; set; }

        public DateTime? SSCDFailMoment { get; set; }

        public Guid? SSCDFailQueueItemId { get; set; }

        public int UsedFailedCount { get; set; }

        public DateTime? UsedFailedMomentMin { get; set; }

        public DateTime? UsedFailedMomentMax { get; set; }

        public Guid? UsedFailedQueueItemId { get; set; }
    }
}