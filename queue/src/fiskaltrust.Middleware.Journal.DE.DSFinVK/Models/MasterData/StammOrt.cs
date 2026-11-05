using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData
{
#pragma warning disable CA1707
    public class StammOrt : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(3)]
        public string LOC_NAME { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(4)]
        public string LOC_STRASSE { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(10)]
        [Index(5)]
        public string LOC_PLZ { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(62)]
        [Index(6)]
        public string LOC_ORT { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(3)]
        [Index(7)]
        public string LOC_LAND { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(15)]
        [Index(8)]
        public string LOC_USTID { get; set; }
    }
}
