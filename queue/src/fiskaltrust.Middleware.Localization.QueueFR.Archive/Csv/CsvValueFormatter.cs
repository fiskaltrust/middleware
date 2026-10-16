using System;
using System.Globalization;

namespace fiskaltrust.Middleware.Localization.QueueFR.Archive.Csv
{
    /// <summary>
    /// Culture-invariant text for a CSV cell. Moments are written with the round-trip pattern "o", which
    /// keeps the full tick precision: the v1 archives used the "G" pattern and lost the milliseconds
    /// (market-fr#127).
    /// </summary>
    public static class CsvValueFormatter
    {
        /// <summary>A case or state value as <c>0x</c> plus 16 upper-case hex digits (archive-v2 field contract).</summary>
        public static string Hex(long value) => "0x" + value.ToString("X16", CultureInfo.InvariantCulture);


        public static string Format(object? value) => value switch
        {
            null => string.Empty,
            string s => s,
            DateTime d => d.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset d => d.ToString("o", CultureInfo.InvariantCulture),
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            float f => f.ToString("R", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            Guid g => g.ToString("D"),
            Enum e => Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            byte[] bytes => Convert.ToBase64String(bytes),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
