using System;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.Csv.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MaxLengthAttribute : Attribute
    {
        public int Length { get; }
        public MaxLengthAttribute(int length) => Length = length;
    }
}
