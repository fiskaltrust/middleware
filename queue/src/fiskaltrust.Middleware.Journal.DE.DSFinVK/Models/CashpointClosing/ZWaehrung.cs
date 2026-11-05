using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.CashpointClosing
{
#pragma warning disable CA1707
    public class ZWaehrung : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(3)]
        [Index(3)]
        public string ZAHLART_WAEH { get; set; }

        [Precision(Precisions.CurrencyAmount)]
        [Index(4)]
        public decimal ZAHLART_BETRAG_WAEH { get; set; }
    }
}
