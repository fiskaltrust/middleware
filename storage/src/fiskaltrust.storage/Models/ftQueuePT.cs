using System;

namespace fiskaltrust.storage.V0
{
    public class ftQueuePT
    {
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

        public long TimeStamp { get; set; }

        /*
         * The following fields should probably be moved to a different config
         */
        public string IssuerTIN { get; set; }

        public NumeratorStorage NumeratorStorage { get; set; }
    }

    public class NumeratorStorage
    {
        public NumberSeries InvoiceSeries { get; set; }
        public NumberSeries SimplifiedInvoiceSeries { get; set; }
        public NumberSeries CreditNoteSeries { get; set; }
        public NumberSeries HandWrittenFSSeries { get; set; }
        public NumberSeries ProFormaSeries { get; set; }
        public NumberSeries PaymentSeries { get; set; }
        public NumberSeries BudgetSeries { get; set; }
        public NumberSeries TableChecqueSeries { get; set; }
    }

    public class NumberSeries
    {
        public string TypeCode { get; set; }
        public string ATCUD { get; set; }
        public string Series { get; set; }
        public string Identifier => $"{TypeCode} {Series}";
        public long Numerator { get; set; }     
        public string LastHash { get; set; }
        public DateTime? LastCbReceiptMoment { get; set; }
    }
}
