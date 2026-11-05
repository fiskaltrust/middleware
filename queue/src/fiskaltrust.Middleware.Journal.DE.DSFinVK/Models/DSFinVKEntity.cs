using System;
using CsvHelper.Configuration.Attributes;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Models
{
#pragma warning disable CA1707
    public abstract class DSFinVKEntity
    {
        [TypeConverter(typeof(StringLengthConverter))]
        [MaxLength(50)]
        [Index(0)]
        public string Z_KASSE_ID { get; set; }

        [Index(1)]
        public DateTime Z_ERSTELLUNG { get; set; }

        [Index(2)]
        public long Z_NR { get; set; }
    }
}
