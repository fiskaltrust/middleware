using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData
{
#pragma warning disable CA1707
    public class StammTSE : DSFinVKEntity
    {
        [Index(3)]
        public int TSE_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(68)]
        [Index(4)]
        public string TSE_SERIAL { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(21)]
        [Index(5)]
        public string TSE_SIG_ALGO { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(31)]
        [Index(6)]
        public string TSE_ZEITFORMAT { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(5)]
        [Index(7)]
        public string TSE_PD_ENCODING { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(512)]
        [Index(8)]
        public string TSE_PUBLIC_KEY { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(1000)]
        [Index(9)]
        public string TSE_ZERTIFIKAT_I { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(1000)]
        [Index(10)]
        public string TSE_ZERTIFIKAT_II { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(1000)]
        [Index(11)]
        public string TSE_ZERTIFIKAT_III { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(1000)]
        [Index(12)]
        public string TSE_ZERTIFIKAT_IV { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(1000)]
        [Index(13)]
        public string TSE_ZERTIFIKAT_V { get; set; }
    }
}
