namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.SerialNumbers.Models
{
    public class SerialNumberRecord
    {
        public byte[] SerialNumber { get; set; }
        public bool IsUsedForTransactionLogs { get; set; }
        public bool IsUsedForSystemLogs { get; set; }
        public bool IsUsedForAuditLogs { get; set; }
    }
}