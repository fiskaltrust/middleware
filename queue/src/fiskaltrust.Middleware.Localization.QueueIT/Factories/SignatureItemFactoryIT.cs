using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts.Factories;
using fiskaltrust.storage.V0;


namespace fiskaltrust.Middleware.Localization.QueueIT.Factories
{
    public class SignatureItemFactoryIT : SignatureItemFactory
    {
        public override long CountryBaseState => 0x4954000000000000;

        public SignatureItemFactoryIT() { 
        }

        protected static NumberFormatInfo GetCurrencyFormatter()
        {
            return new NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = "",
                CurrencyDecimalDigits = 2
            };
        }
        public SignaturItem []  CreatePosReceiptSignatures(FiscalReceiptResponse fiscalReceiptResponse)
        { 
            return new SignaturItem[]
            {
                new SignaturItem
                {
                    Caption = "ZRepNumber",
                    Data = fiscalReceiptResponse.Number.ToString(),
                    ftSignatureFormat = 0x01,
                    ftSignatureType = CountryBaseState
                },
                new SignaturItem
                {
                    Caption = "Amount",
                    Data = fiscalReceiptResponse.Amount.ToString(GetCurrencyFormatter()),
                    ftSignatureFormat = 0x01,
                    ftSignatureType = CountryBaseState
                },
                new SignaturItem
                {
                    Caption = "TimeStamp",
                    Data = fiscalReceiptResponse.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    ftSignatureFormat = 0x01,
                    ftSignatureType = CountryBaseState
                }
            };
        }
    }
}
