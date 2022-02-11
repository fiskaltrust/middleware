using System;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers
{
    public static class TimestampHelper
    {

        public static DateTime ToDateTime(this UInt64 timestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
        }


        public static UInt64 ToTimestamp(this DateTime time)
        {
            return (UInt64) time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }


        public static string ToLogTimeFormat(this SeSyncVariant timeSyncVariant)
        {
            return timeSyncVariant switch
            {
                SeSyncVariant.utcTime => "utcTime",
                SeSyncVariant.generalizedTime => "generalizedTime",
                SeSyncVariant.unixTime => "unixTime",
                _ => "noInput",
            };
        }


    }
}
