using System;

namespace fiskaltrust.Middleware.SCU.DE.FiskalyCertified.Models
{
    public class SplitExportStateData
    {
        public Guid ParentExportId { get; set; }
        public Guid ExportId { get; set; }
        public ExportStateData ExportStateData { get; set; }
        public long From { get; set; }
        public long To { get; set; }
    }
}
