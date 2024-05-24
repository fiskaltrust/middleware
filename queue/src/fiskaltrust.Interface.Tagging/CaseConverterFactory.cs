using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v0;
using fiskaltrust.Interface.Tagging.AT;
using fiskaltrust.Interface.Tagging.DE;
using fiskaltrust.Interface.Tagging.FR;
using fiskaltrust.Interface.Tagging.Interfaces;

namespace fiskaltrust.Interface.Tagging
{
    internal class CaseConverterFactory : ICaseConverterFactory
    {
        public ICaseConverter CreateInstance(long ftReceiptCase) 
        {
            return (0xFFFF000000000000 & (ulong) ftReceiptCase) switch
            {
                0x4445000000000000 => new CaseConverterDE(),
                0x4154000000000000 => new CaseConverterAT(),
                0x4652000000000000 => new CaseConverterFR(),
                _ => throw new Exception("The recieved code is not valid")
            };
            ;
        }
    }
}
