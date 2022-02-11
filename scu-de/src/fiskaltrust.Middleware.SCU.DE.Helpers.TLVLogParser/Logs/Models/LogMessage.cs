using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Logs.Models
{
    public class LogMessage
    {
        public int Version { get; set; }
        public string SerialNumber { get; set; }
        public SignatureAlgorithm SignatureAlgorithm { get; set; }
        public string SeAuditData { get; set; }
        public ulong SignaturCounter { get; set; }
        public DateTime LogTime { get; set; }
        public string LogTimeFormat { get; set; }
        public string SignaturValueBase64 { get; set; }
        public List<byte> RawData { get; set; }
        public string FileName { get; set; }
    }
}
