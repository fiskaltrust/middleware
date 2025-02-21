using System;

namespace fiskaltrust.Middleware.Interface.Client.Soap.Extensions
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan Double(this TimeSpan timeSpan) => timeSpan.Add(timeSpan);
    }
}
