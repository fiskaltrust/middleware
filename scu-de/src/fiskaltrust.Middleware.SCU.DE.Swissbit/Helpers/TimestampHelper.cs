using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers
{
    public static class TimestampHelper
    {
        // TODO add TimeFormat as parameter and share it over multiple scu implementations

        public static DateTime ToDateTime(this ulong timestamp) => new DateTime(1970, 1, 1).AddSeconds(timestamp);

        public static ulong ToTimestamp(this DateTime time) => (ulong) time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
