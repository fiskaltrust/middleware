using System;
using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Factories;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignatureItemFactoryIT : SignatureItemFactory
    {
        public override long CountryBaseState => 0x4954000000000000;

        public SignatureItemFactoryIT()
        {
        }

        protected static NumberFormatInfo CurrencyFormatter = new()
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = "",
            CurrencyDecimalDigits = 2
        };
    }
}
