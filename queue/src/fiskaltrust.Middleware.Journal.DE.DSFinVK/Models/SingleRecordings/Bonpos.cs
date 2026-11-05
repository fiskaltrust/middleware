using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings
{
#pragma warning disable CA1707
    public class Bonpos : DSFinVKEntity
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
        public string GUTSCHEIN_NR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(255)]
        [Index(6)]
        public string ARTIKELTEXT { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(7)]
        public string POS_TERMINAL_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(30)]
        [Index(8)]
        public string GV_TYP { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(9)]
        public string GV_NAME { get; set; }

        [Index(10)]
        public bool INHAUS { get; set; }

        [Index(11)]
        public bool P_STORNO { get; set; }

        [Index(12)]
        public long AGENTUR_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(13)]
        public string ART_NR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(14)]
        public string GTIN { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(15)]
        public string WARENGR_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(16)]
        public string WARENGR { get; set; }

        [Precision(Precisions.Quantity)]
        [Index(17)]
        public decimal MENGE { get; set; }

        [Precision(Precisions.Quantity)]
        [Index(18)]
        public decimal? FAKTOR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(19)]
        public string EINHEIT { get; set; }

        [Precision(Precisions.PricePerUnit)]
        [Index(20)]
        public decimal? STK_BR { get; set; }
    }
}
