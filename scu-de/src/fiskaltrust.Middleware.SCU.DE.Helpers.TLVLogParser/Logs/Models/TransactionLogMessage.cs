using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models
{
    public class TransactionLogMessage : LogMessage
    {
        public string OperationType { get; set; }
        public string ClientId { get; set; }
        public string ProcessDataBase64 { get; set; }
        public string ProcessType { get; set; }
        public List<byte> AdditionalExternalData { get; set; }
        public ulong TransactionNumber { get; set; }
        public List<byte> AdditionalInternalData { get; set; }
    }
}
