using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1;
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
        public IEnumerable<SignaturItem> CreatePosReceiptSignatures(string data)
        { 
            return new List<SignaturItem>
            {
                new SignaturItem
                {
                    Caption = "ToDo",
                    Data = data,
                    ftSignatureFormat = 0x01,
                    ftSignatureType = CountryBaseState
                }
            };
        }
    }
}
