using System;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudAndroid.Helpers
{
    public static class DateTimeUtil
    {
        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime DateTimeFromUnixTimestampMillis(long millis)
        {
            return _unixEpoch.AddMilliseconds(millis);
        }

        public static DateTime DateTimeFromUnixTimestampSeconds(long seconds)
        {
            return _unixEpoch.AddSeconds(seconds);
        }
    }
}