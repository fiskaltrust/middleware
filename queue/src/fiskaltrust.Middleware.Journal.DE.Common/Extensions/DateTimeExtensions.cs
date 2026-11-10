using System;

namespace fiskaltrust.Middleware.Journal.DE.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool Between(this DateTime input, DateTime fromInclusive, DateTime toExclusive) => input >= fromInclusive && input < toExclusive;
    }
}
