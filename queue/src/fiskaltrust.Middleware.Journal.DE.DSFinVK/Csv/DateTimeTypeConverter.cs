using System;
using System.Globalization;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv
{
    public class DateTimeTypeConverter : DefaultTypeConverter
    {
        private const string FORMAT_STRING = "yyyy-MM-dd'T'HH:mm:ss'Z'";
        private const string FORMAT_STRING_MS = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (!(value is DateTime dateTime))
            {
                return base.ConvertToString(value, row, memberMapData);
            }

            if (memberMapData?.Member != null && memberMapData.Member.GetCustomAttributes(typeof(DateTimePrecisionAttribute), false).FirstOrDefault() is DateTimePrecisionAttribute attribute && attribute.HasMilliseconds)
            {
                return dateTime.ToString(FORMAT_STRING_MS, CultureInfo.InvariantCulture);
            }
            else
            {
                return dateTime.ToString(FORMAT_STRING, CultureInfo.InvariantCulture);
            }
        }
    }
}
