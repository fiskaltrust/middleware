using System;
using System.Collections.Generic;
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
        public SignaturItem []  CreatePosReceiptSignatures(FiscalReceiptResponse fiscalReceiptResponse)
        { 
            return new SignaturItem[]
            {
                new SignaturItem
                {
                    Caption = "ZRepNumber",
                    Data = fiscalReceiptResponse.ZRepNumber.ToString(),
                    ftSignatureFormat = 0x01,
                    ftSignatureType = CountryBaseState
                },
                new SignaturItem
                {
                    Caption = "Amount",
                    Data = fiscalReceiptResponse.Amount.ToString(),
                    ftSignatureFormat = 0x01,
                    ftSignatureType = CountryBaseState
                },
                new SignaturItem
                {
                    Caption = "TimeStamp",
                    Data = fiscalReceiptResponse.TimeStamp.ToString(),
                    ftSignatureFormat = 0x01,
                    ftSignatureType = CountryBaseState
                }
            };
        }
    }
}
