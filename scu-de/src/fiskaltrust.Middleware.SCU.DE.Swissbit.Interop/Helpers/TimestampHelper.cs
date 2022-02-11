using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.Helpers
{
    public static class TimestampHelper
    {
        // TODO add TimeFormat as parameter and share it over multiple scu implementations

        public static DateTime ToDateTime(this UInt64 timestamp) => new DateTime(1970, 1, 1).AddSeconds(timestamp);

        public static UInt64 ToTimestamp(this DateTime time) => (UInt64) time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
