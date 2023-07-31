using System;
using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Factories;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueES.Factories
{
    public class SignatureItemFactoryES : SignatureItemFactory
    {
        public override long CountryBaseState => 0x4553000000000000;

        public SignatureItemFactoryES() { 
        }

        protected static NumberFormatInfo CurrencyFormatter = new ()
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = "",
            CurrencyDecimalDigits = 2
        };

        public SignaturItem[] CreatePosReceiptSignatures(DailyClosingResponse dailyClosingResponse)
        {
            return new SignaturItem[]
            {
                // TODO 
            };
        }

        public SignaturItem []  CreatePosReceiptSignatures(FiscalReceiptResponse fiscalReceiptResponse)
        { 
            return new SignaturItem[]
            {
                // TODO
            };
        }
    }
}
