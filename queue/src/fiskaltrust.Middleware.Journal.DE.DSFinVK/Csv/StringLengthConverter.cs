using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;
using fiskaltrust.Middleware.Journal.DE.Common.Extensions;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv
{
    public class StringLengthConverter : DefaultTypeConverter
    {
        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is string valueStr)
            {
                if (memberMapData.Member.GetCustomAttributes(typeof(MaxLengthAttribute), false).FirstOrDefault() is MaxLengthAttribute attribute)
                {
                    valueStr = valueStr.SafeSubstring(0, attribute.Length);
                }
                return base.ConvertToString(valueStr, row, memberMapData);
            }
            return base.ConvertToString(value, row, memberMapData);
        }

        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) => text;
    }
}
