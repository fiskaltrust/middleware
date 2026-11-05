using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings
{
#pragma warning disable CA1707
    public class BonposUst : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(3)]
        public string BON_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(4)]
        public string POS_ZEILE { get; set; }

        [Index(5)]
        public long UST_SCHLUESSEL { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(6)]
        public decimal POS_BRUTTO { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(7)]
        public decimal POS_NETTO { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(8)]
        public decimal POS_UST { get; set; }
    }
}
