using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData
{
#pragma warning disable CA1707
    public class StammAbschluss : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(25)]
        [Index(3)]
        public string Z_BUCHUNGSTAG { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(10)]
        [Index(4)]
        public string TAXONOMIE_VERSION { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(5)]
        public string Z_START_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(6)]
        public string Z_ENDE_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(7)]
        public string NAME { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(8)]
        public string STRASSE { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(10)]
        [Index(9)]
        public string PLZ { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(62)]
        [Index(10)]
        public string ORT { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(3)]
        [Index(11)]
        public string LAND { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(20)]
        [Index(12)]
        public string STNR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(15)]
        [Index(13)]
        public string USTID { get; set; }

        [Precision(Precisions.CurrencyAmount)]
        [Index(14)]
        public decimal Z_SE_ZAHLUNGEN { get; set; }

        [Precision(Precisions.CurrencyAmount)]
        [Index(15)]
        public decimal Z_SE_BARZAHLUNGEN { get; set; }
    }
}
