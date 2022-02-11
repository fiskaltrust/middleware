using System;
using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models
{
    public class SlotInfo
    {
        public SlotStates SlotStatus { get; set; }
        public SlotTseStates TseStatus { get; set; }
        public long CryptoVendor { get; set; }
        public string CryptoInfo { get; set; }
        public long Capacity { get; set; }
        public long FreeSpace { get; set; }
        public DateTime CertExpDate { get; set; }
        public long AvailSig { get; set; }
        public string SigAlgorithm { get; set; }
        public long CryptoFwType { get; set; }
        public string TSEDescription { get; set; }
    }
}