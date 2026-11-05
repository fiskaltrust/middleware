using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv
{
    public class BoolTypeConverter : DefaultTypeConverter
    {
        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (!(value is bool boolValue))
            {
                return base.ConvertToString(value, row, memberMapData);
            }

            return boolValue ? "1" : "0";
        }
    }
}
