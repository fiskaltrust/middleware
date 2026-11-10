using System;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PrecisionAttribute : Attribute
    {
        public int Precision { get; }

        public PrecisionAttribute(int precision) => Precision = precision;
    }
}
