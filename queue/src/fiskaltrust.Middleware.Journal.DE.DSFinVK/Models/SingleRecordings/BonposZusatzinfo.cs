using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings
{
#pragma warning disable CA1707
    public class BonposZusatzinfo : DSFinVKEntity
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
        [MaxLength(50)]
        [Index(5)]
        public string ZI_ART_NR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(6)]
        public string ZI_GTIN { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(7)]
        public string ZI_NAME { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(8)]
        public string ZI_WARENGR_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(9)]
        public string ZI_WARENGR { get; set; }

        [Precision(Precisions.Quantity)]
        [Index(10)]
        public decimal ZI_MENGE { get; set; }

        [Precision(Precisions.Quantity)]
        [Index(11)]
        public decimal ZI_FAKTOR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(12)]
        public string ZI_EINHEIT { get; set; }

        [Index(13)]
        public int ZI_UST_SCHLUESSEL { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(14)]
        public decimal ZI_BASISPREIS_BRUTTO { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(15)]
        public decimal ZI_BASISPREIS_NETTO { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount)]
        [Index(16)]
        public decimal ZI_BASISPREIS_UST { get; set; }
    }
}
