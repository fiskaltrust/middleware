using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData
{
#pragma warning disable CA1707
    public class StammUst : DSFinVKEntity
    {
        [Index(3)]
        public int UST_SCHLUESSEL { get; set; }

        [Precision(Precisions.VatRate)]
        [Index(4)]
        public decimal UST_SATZ { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(55)]
        [Index(5)]
        public string UST_BESCHR { get; set; }
    }
}
