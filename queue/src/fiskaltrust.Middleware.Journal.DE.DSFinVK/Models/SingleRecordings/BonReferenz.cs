using System;
using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.SingleRecordings
{
#pragma warning disable CA1707
    public class BonReferenz : DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(3)]
        public string BON_ID { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(4)]
        public string POS_ZEILE { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(20)]
        [Index(5)]
        public string REF_TYP { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(6)]
        public string REF_NAME { get; set; }

        [Index(7)]
        public DateTime? REF_DATUM { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(8)]
        public string REF_Z_KASSE_ID { get; set; }

        [Index(9)]
        public long REF_Z_NR { get; set; }

        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(40)]
        [Index(10)]
        public string REF_BON_ID { get; set; }
    }
}
