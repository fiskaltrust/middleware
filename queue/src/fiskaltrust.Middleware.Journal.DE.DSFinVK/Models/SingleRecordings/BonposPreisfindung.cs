using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings
{
#pragma warning disable CA1707
    public class BonposPreisfindung : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(3)]
        public string BON_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(4)]
        public string POS_ZEILE { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(20)]
        [Index(5)]
        public string TYP { get; set; }

        [Index(6)]
        public int UST_SCHLUESSEL { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(7)]
        public decimal PF_BRUTTO { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(8)]
        public decimal PF_NETTO { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(9)]
        public decimal PF_UST { get; set; }
    }
}
