using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v0;
using fiskaltrust.Interface.Tagging.DE;
using fiskaltrust.Interface.Tagging.Interfaces;

namespace fiskaltrust.Interface.Tagging
{
    internal class CaseConverterFactory : ICaseConverterFactory
    {
        public ICaseConverter ICaseConverterFactory.CreateInstance(long ftReceiptCase) 
        {
            return (0xFFFF000000000000 & (ulong) ftReceiptCase) switch
            {
                0x4445000000000000 => new CaseConverterDE(),
                0x4652000000000000 => "FR",
                0x4D45000000000000 => "ME",
                0x4954000000000000 => "IT",
                _ => "AT",
            };
        }
    }
}
