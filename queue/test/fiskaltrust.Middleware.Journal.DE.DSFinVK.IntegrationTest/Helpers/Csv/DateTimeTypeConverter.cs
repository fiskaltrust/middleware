using System;
using System.Globalization;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.IntegrationTest.Helpers.Csv
{
    public class DateTimeTypeConverter : DefaultTypeConverter
    {
        private const string FORMAT_STRING = "yyyy-MM-dd'T'HH:mm:ss'Z'";
        private const string FORMAT_STRING_MS = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (memberMapData?.Member != null && memberMapData.Member.GetCustomAttributes(typeof(DateTimePrecisionAttribute), false).FirstOrDefault() is DateTimePrecisionAttribute attribute && attribute.HasMilliseconds)
            {
                return DateTime.ParseExact(text, FORMAT_STRING_MS, CultureInfo.InvariantCulture);
            }
            else
            {
                return DateTime.ParseExact(text, FORMAT_STRING, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            }
        }
    }
}
