using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models
{
    public class SystemLogMessage : LogMessage
    {
        public string OperationType { get; set; }
        public List<byte> SystemOperationData { get; set; }
        public List<byte> AdditionalInternalData { get; set; }
    }
}
