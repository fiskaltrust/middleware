using System;
using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings
{
#pragma warning disable CA1707
    public class TseTransaktion : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(3)]
        public string BON_ID { get; set; }

        [Index(4)]
        public int TSE_ID { get; set; }

        [Index(5)]
        public long TSE_TANR { get; set; }

        [DateTimePrecision(HasMilliseconds = true)]
        [Index(6)]
        public DateTime TSE_TA_START { get; set; }

        [DateTimePrecision(HasMilliseconds = true)]
        [Index(7)]
        public DateTime TSE_TA_ENDE { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(30)]
        [Index(8)]
        public string TSE_TA_VORGANGSART { get; set; }

        [Index(9)]
        public long TSE_TA_SIGZ { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(512)]
        [Index(10)]
        public string TSE_TA_SIG { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(200)]
        [Index(11)]
        public string TSE_TA_FEHLER { get; set; }

        [Index(12)]
        public string TSE_VORGANGSDATEN { get; set; }
    }
}
