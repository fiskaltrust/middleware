using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings
{
#pragma warning disable CA1707
    public class BonkopfZahlart : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(3)]
        public string BON_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(25)]
        [Index(4)]
        public string ZAHLART_TYP { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(5)]
        public string ZAHLART_NAME { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(3)]
        [Index(6)]
        public string ZAHLWAEH_CODE { get; set; }

        [Precision(Precisions.CurrencyAmount)]
        [Index(7)]
        public decimal? ZAHLWAEH_BETRAG { get; set; }

        [Precision(Precisions.CurrencyAmount)]
        [Index(8)]
        public decimal? BASISWAEH_BETRAG { get; set; }
    }
}
