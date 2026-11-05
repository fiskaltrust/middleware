using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.IntegrationTest.Helpers.Csv
{
    public class DecimalTypeConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var info = CultureInfo.InvariantCulture.Clone() as CultureInfo;
            info.NumberFormat.NumberDecimalSeparator = ",";

            return decimal.Parse(text, info);
        }
    }
}
