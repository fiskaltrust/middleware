using System;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.v2.v2
{
    public class ftQueuePT : QueueLocalization, ICountrySpecificQueue
    {
        public Guid ftQueueId => ftQueuePTId;

        public Guid? ftSignaturCreationUnitId => ftSignaturCreationUnitPTId;

        public Guid ftQueuePTId { get; set; }

        public Guid? ftSignaturCreationUnitPTId { get; set; }

        public string LastHash { get; set; }

        public string LastSignature { get; set; }

        public string CashBoxIdentification { get; set; }

        public int SSCDFailCount { get; set; }

        public DateTime? SSCDFailMoment { get; set; }

        public Guid? SSCDFailQueueItemId { get; set; }

        public int UsedFailedCount { get; set; }

        public DateTime? UsedFailedMomentMin { get; set; }

        public DateTime? UsedFailedMomentMax { get; set; }

        public Guid? UsedFailedQueueItemId { get; set; }

        /*
         * The following fields should probably be moved to a different config
         */
        public string TaxRegion { get; set; }
        public string IssuerTIN { get; set; }
        public string ATCUD { get; set; }
        public string SimplifiedInvoiceSeries { get; set; }
        public long SimplifiedInvoiceSeriesNumerator { get; set; }
        public string SoftwareCertificateNumber { get; set; }
    }
}
