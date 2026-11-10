using System;
using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings
{
#pragma warning disable CA1707
    public class Bonkopf : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(3)]
        public string BON_ID { get; set; }

        [Index(4)]
        public long BON_NR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(30)]
        [Index(5)]
        public string BON_TYP { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(6)]
        public string BON_NAME { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(7)]
        public string TERMINAL_ID { get; set; }

        [Index(8)]
        public bool BON_STORNO { get; set; }

        [Index(9)]
        public DateTime BON_START { get; set; }

        [Index(10)]
        public DateTime BON_ENDE { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(11)]
        public string BEDIENER_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(12)]
        public string BEDIENER_NAME { get; set; }

        [Precision(Precisions.CurrencyAmount)]
        [Index(13)]
        public decimal UMS_BRUTTO { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(14)]
        public string KUNDE_NAME { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(15)]
        public string KUNDE_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(16)]
        public string KUNDE_TYP { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(60)]
        [Index(17)]
        public string KUNDE_STRASSE { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(10)]
        [Index(18)]
        public string KUNDE_PLZ { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(62)]
        [Index(19)]
        public string KUNDE_ORT { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(3)]
        [Index(20)]
        public string KUNDE_LAND { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(15)]
        [Index(21)]
        public string KUNDE_USTID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(255)]
        [Index(22)]
        public string BON_NOTIZ { get; set; }
    }
}
