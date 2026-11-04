using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData
{
#pragma warning disable CA1707
    public class StammTerminal : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(3)]
        public string TERMINAL_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(4)]
        public string TERMINAL_BRAND { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(5)]
        public string TERMINAL_MODELL { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(70)]
        [Index(6)]
        public string TERMINAL_SERIENNR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(7)]
        public string TERMINAL_SW_BRAND { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(8)]
        public string TERMINAL_SW_VERSION { get; set; }
    }
}
