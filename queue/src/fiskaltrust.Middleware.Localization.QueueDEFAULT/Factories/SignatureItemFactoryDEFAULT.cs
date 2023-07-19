﻿using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
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
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = "",
            CurrencyDecimalDigits = 2
        };
        
    public List<SignaturItem> GetSignaturesForPosReceiptTransaction(string cashBoxId, string receiptId, decimal sumOfPayItems, string previousHash)
    {
        var signatures = new List<SignaturItem>
        {
            CreateQrCodeSignature("www.fiskaltrust.eu", $"{cashBoxId}_{receiptId}_{sumOfPayItems}_{previousHash}")
        };

        return signatures;
    }

    public SignaturItem CreateQrCodeSignature(string caption, string data) => new()
        {
            Caption = caption,
            ftSignatureFormat = (long)SignaturItem.Formats.QR_Code,
            Data = data,
            ftSignatureType = 0x0
        };
    }
}
