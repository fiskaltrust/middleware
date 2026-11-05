using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData
{
#pragma warning disable CA1707
    public class StammAgentur : DSFinVKEntity
    {
        [Index(3)]
        public long AGENTUR_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(4)]
        public string AGENTUR_NAME { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(5)]
        public string AGENTUR_STRASSE { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(10)]
        [Index(6)]
        public string AGENTUR_PLZ { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(62)]
        [Index(7)]
        public string AGENTUR_ORT { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(3)]
        [Index(8)]
        public string AGENTUR_LAND { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(20)]
        [Index(9)]
        public string AGENTUR_STNR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(15)]
        [Index(10)]
        public string AGENTUR_USTID { get; set; }
    }
}
