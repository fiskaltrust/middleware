using System.Globalization;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

internal static class CountrySegment
{
    public static bool TryParse(string? receiptIdentification, out string series, out long aa)
    {
        // The country segment is everything after the first "#":  "ft{N:X}#{series}-{aa}".
        // The series itself may contain dashes, so the aa is split off at the last one.
        series = string.Empty;
        aa = 0;
        if (string.IsNullOrEmpty(receiptIdentification))
        {
            return false;
        }
        var hashIdx = receiptIdentification.IndexOf('#');
        if (hashIdx < 0 || hashIdx >= receiptIdentification.Length - 1)
        {
            return false;
        }
        var segment = receiptIdentification.Substring(hashIdx + 1);
        var dashIdx = segment.LastIndexOf('-');
        if (dashIdx <= 0 || dashIdx >= segment.Length - 1)
        {
            return false;
        }
        if (!long.TryParse(segment.Substring(dashIdx + 1), NumberStyles.None, CultureInfo.InvariantCulture, out aa))
        {
            return false;
        }
        series = segment.Substring(0, dashIdx);
        return true;
    }
}
