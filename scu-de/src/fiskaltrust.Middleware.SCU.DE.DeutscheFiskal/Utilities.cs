using System;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal
{
    public static class Utilities
    {
        public static DateTime FromUnixTime(this long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static ulong ToUnixTime(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (ulong) Convert.ToInt64((date - epoch).TotalSeconds);
        }
    }
}
