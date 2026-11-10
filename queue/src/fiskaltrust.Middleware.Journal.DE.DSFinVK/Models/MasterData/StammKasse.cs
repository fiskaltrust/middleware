using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData
{
#pragma warning disable CA1707
    public class StammKasse : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(3)]
        public string KASSE_BRAND { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(4)]
        public string KASSE_MODELL { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(70)]
        [Index(5)]
        public string KASSE_SERIENNR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(6)]
        public string KASSE_SW_BRAND { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(7)]
        public string KASSE_SW_VERSION { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(3)]
        [Index(8)]
        public string KASSE_BASISWAEH_CODE { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(1)]
        [Index(9)]
        public string KEINE_UST_ZUORDNUNG { get; set; }
    }
}
