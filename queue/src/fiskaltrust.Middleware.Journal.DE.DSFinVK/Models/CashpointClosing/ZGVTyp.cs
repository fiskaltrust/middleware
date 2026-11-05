using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.CashpointClosing
{
#pragma warning disable CA1707
    public class ZGVTyp : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(30)]
        [Index(3)]
        public string GV_TYP { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(4)]
        public string GV_NAME { get; set; }

        [Index(5)]
        public long AGENTUR_ID { get; set; }

        [Index(6)]
        public int UST_SCHLUESSEL { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(7)]
        public decimal Z_UMS_BRUTTO { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(8)]
        public decimal Z_UMS_NETTO { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(9)]
        public decimal Z_UST { get; set; }
    }
}
