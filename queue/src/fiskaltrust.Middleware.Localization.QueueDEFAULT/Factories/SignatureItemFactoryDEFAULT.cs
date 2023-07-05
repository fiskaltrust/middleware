using System;
using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Factories;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories
{
    public class SignatureItemFactoryDEFAULT : SignatureItemFactory
    {
        public override long CountryBaseState => 000000000;

        public SignatureItemFactoryDEFAULT() { 
        }

        protected static NumberFormatInfo CurrencyFormatter = new ()
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = "",
            CurrencyDecimalDigits = 2
        };

        public SignaturItem[] CreateSignatures(DailyClosingResponse dailyClosingResponse) //CreateSignatures then remove the other functions
        {
            return new SignaturItem[]
            {
                
            };
        }
    }
}
