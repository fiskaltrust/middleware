using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv
{
    public class CsvGenerator
    {
        public async Task WriteAsync<T>(string path, IEnumerable<T> records)
        {
            using (var writer = new StreamWriter(path))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Configuration.TypeConverterCache.AddConverter<bool>(new BoolTypeConverter());
                csv.Configuration.TypeConverterCache.AddConverter<DateTime>(new DateTimeTypeConverter());
                csv.Configuration.TypeConverterCache.AddConverter<decimal>(new DecimalTypeConverter());

                csv.Configuration.Delimiter = ";";
                csv.Configuration.NewLine = NewLine.CRLF;

                await csv.WriteRecordsAsync(records);
            }
        }
    }
}
