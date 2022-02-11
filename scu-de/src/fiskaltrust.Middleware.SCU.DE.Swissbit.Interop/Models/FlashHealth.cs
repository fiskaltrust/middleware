using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.Models
{
    public class FlashHealth
    {
        public UInt32 UncorrectableEccErrors { get; set; }
        public byte PercentageRemainingSpareBlocks { get; set; }
        public byte PercentageRemainingEraseCounts { get; set; }
        public byte PercentageRemainingTenYearDataRetention { get; set; }
        public bool NeedsReplacement { get; set; }
    }
}
