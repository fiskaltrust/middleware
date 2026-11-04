using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings
{
#pragma warning disable CA1707
    public class BonkopfAbrKreis : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(3)]
        public string BON_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(4)]
        public string ABRECHNUNGSKREIS { get; set; }
    }
}
