using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.Models
{
    public class WormEntry
    {
        public bool IsValid { get; set; }
        public UInt32 Id { get; set; }
        public NativeFunctionPointer.WormEntryType Type { get; set; }
        public string LogMessageBase64 { get; set; }
        public string ProcessDataBase64 { get; set; }
        public string LogMessageCertificateBase64 { get; set; }
    }
}
