using System;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers
{
    public static class TimestampHelper
    {
        public static DateTime LinuxTimestampToDateTime(this ulong timestamp) => new DateTime(1970, 1, 1).AddSeconds(timestamp);

        public static ulong ToLinuxTimestamp(this DateTime time) => (ulong) time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
