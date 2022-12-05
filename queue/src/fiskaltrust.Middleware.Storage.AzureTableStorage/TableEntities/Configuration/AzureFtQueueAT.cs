using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration
{
    public class AzureFtQueueAT : BaseTableEntity
    {
        public string LastSignatureCertificateSerialNumber { get; set; }
        public string LastSignatureZDA { get; set; }
        public string LastSignatureHash { get; set; }
        public DateTime? MessageMoment { get; set; }
        public int MessageCount { get; set; }
        public Guid? UsedMobileQueueItemId { get; set; }
        public DateTime? UsedMobileMoment { get; set; }
        public int UsedMobileCount { get; set; }
        public Guid? UsedFailedQueueItemId { get; set; }
        public DateTime? UsedFailedMomentMax { get; set; }
        public DateTime? UsedFailedMomentMin { get; set; }
        public int UsedFailedCount { get; set; }
        public long ftCashNumerator { get; set; }
        public DateTime? SSCDFailMessageSent { get; set; }
        public DateTime? SSCDFailMoment { get; set; }
        public int SSCDFailCount { get; set; }
        public Guid? LastSettlementQueueItemId { get; set; }
        public DateTime? LastSettlementMoment { get; set; }
        public int LastSettlementMonth { get; set; }
        public string ClosedSystemNote { get; set; }
        public string ClosedSystemValue { get; set; }
        public string ClosedSystemKind { get; set; }
        public bool SignAll { get; set; }
        public string EncryptionKeyBase64 { get; set; }
        public string CashBoxIdentification { get; set; }
        public Guid ftQueueATId { get; set; }
        public Guid? SSCDFailQueueItemId { get; set; }
        public double ftCashTotalizer { get; set; }
        public long TimeStamp { get; set; }
    }
}
