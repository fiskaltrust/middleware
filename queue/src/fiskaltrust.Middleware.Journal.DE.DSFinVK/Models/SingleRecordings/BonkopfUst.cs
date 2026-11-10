using System;
using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings
{
#pragma warning disable CA1707
    public class BonkopfUst : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(3)]
        public string BON_ID { get; set; }

        [Index(4)]
        public int UST_SCHLUESSEL { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount2Digits)]
        [Index(5)]
        public decimal BON_BRUTTO { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount2Digits)]
        [Index(6)]
        public decimal BON_NETTO { get; set; }

        [Precision(Precisions.AggregatedCurrencyAmount2Digits)]
        [Index(7)]
        public decimal BON_UST { get; set; }
    }
}
