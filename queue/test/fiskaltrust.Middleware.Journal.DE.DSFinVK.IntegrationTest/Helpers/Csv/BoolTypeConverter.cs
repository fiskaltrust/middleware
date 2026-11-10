using System;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.IntegrationTest.Helpers.Csv
{
    public class BoolTypeConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) =>
            text switch
            {
                "0" => false,
                "1" => true,
                _ => throw new ArgumentException($"Input '{text}' not valid, must be 1 or 0.")
            };
    }
}
