using System.Globalization;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv
{
    public class DecimalTypeConverter : DefaultTypeConverter
    {
        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (!(value is decimal dec))
            {
                return base.ConvertToString(value, row, memberMapData);
            }

            var info = CultureInfo.InvariantCulture.Clone() as CultureInfo;
            info.NumberFormat.NumberDecimalSeparator = ",";

            if (memberMapData.Member.GetCustomAttributes(typeof(PrecisionAttribute), false).FirstOrDefault() is PrecisionAttribute attribute)
            {
                return dec.ToString($"F{attribute.Precision}", info);
            }
            else
            {
                return dec.ToString("F2", info);
            }
        }
    }
}
