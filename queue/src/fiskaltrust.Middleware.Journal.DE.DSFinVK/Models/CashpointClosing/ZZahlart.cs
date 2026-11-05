using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.CashpointClosing
{
#pragma warning disable CA1707
    public class ZZahlart : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(25)]
        [Index(3)]
        public string ZAHLART_TYP { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(4)]
        public string ZAHLART_NAME { get; set; }

        [Precision(Precisions.CurrencyAmount)]
        [Index(5)]
        public decimal Z_ZAHLART_BETRAG { get; set; }
    }
}
