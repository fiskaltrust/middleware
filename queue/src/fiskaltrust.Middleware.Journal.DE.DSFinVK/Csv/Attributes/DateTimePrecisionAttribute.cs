using System;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DateTimePrecisionAttribute : Attribute
    {
        public bool HasMilliseconds { get; set; }
    }
}
