using System;

namespace fiskaltrust.Middleware.Localization.QueueFR.Models
{
    public class Totals
    {
        public decimal? Totalizer { get; set; } = null;
        public decimal? CITotalNormal { get; set; } = null;
        public decimal? CITotalReduced1 { get; set; } = null;
        public decimal? CITotalReduced2 { get; set; } = null;
        public decimal? CITotalReducedS { get; set; } = null;
        public decimal? CITotalZero { get; set; } = null;
        public decimal? CITotalUnknown { get; set; } = null;
        public decimal? PITotalCash { get; set; } = null;
        public decimal? PITotalNonCash { get; set; } = null;
        public decimal? PITotalInternal { get; set; } = null;
        public decimal? PITotalUnknown { get; set; } = null;
    }
}
