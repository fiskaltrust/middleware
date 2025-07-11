using System;

namespace fiskaltrust.storage.V0
{
    public class ftQueueAT : QueueLocalization
    {
        public Guid ftQueueATId { get; set; }

        public string CashBoxIdentification { get; set; }

        public string EncryptionKeyBase64 { get; set; }

        public bool SignAll { get; set; }

        public string ClosedSystemKind { get; set; }
        public string ClosedSystemValue { get; set; }
        public string ClosedSystemNote { get; set; }

        public int LastSettlementMonth { get; set; }
        public DateTime? LastSettlementMoment { get; set; }

        public Guid? LastSettlementQueueItemId { get; set; }

        public int SSCDFailCount { get; set; }
        public DateTime? SSCDFailMoment { get; set; }
        public Guid? SSCDFailQueueItemId { get; set; }
        public DateTime? SSCDFailMessageSent { get; set; }

        public int UsedFailedCount { get; set; }
        public DateTime? UsedFailedMomentMin { get; set; }
        public DateTime? UsedFailedMomentMax { get; set; }
        public Guid? UsedFailedQueueItemId { get; set; }

        public int UsedMobileCount { get; set; }
        public DateTime? UsedMobileMoment { get; set; }
        public Guid? UsedMobileQueueItemId { get; set; }

        public int MessageCount { get; set; }

        public DateTime? MessageMoment { get; set; }
        //public void MessageCountIncrement();
        //public void MessageCountReset();


        public string LastSignatureHash { get; set; }
        public string LastSignatureZDA { get; set; }
        public string LastSignatureCertificateSerialNumber { get; set; }

        public long ftCashNumerator { get; set; }

        public decimal ftCashTotalizer { get; set; }
        //public long ftDeliveryNumerator { get; set; }
        //public decimal ftDeliveryTotalizer { get; set; }
        //public long ftAgencyNumerator { get; set; }
        //public decimal ftAgencyTotalizer { get; set; }
        //public long ftInternalNumerator { get; set; }
        //public decimal ftInternalTotalizer { get; set; }

    }
}