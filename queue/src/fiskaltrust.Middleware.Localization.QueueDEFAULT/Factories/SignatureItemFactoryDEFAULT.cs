using System;
using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Factories;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories
{
    public class SignatureItemFactoryDEFAULT : SignatureItemFactory
    {
        public override long CountryBaseState => 000000000;

        protected static NumberFormatInfo CurrencyFormatter = new ()
        {
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = "",
            CurrencyDecimalDigits = 2
        };
        
        public List<SignaturItem> GetSignaturesForPosReceiptTransaction(Guid cashBoxId, Guid receiptId,
            decimal sumOfPayItems, string receiptHash, long ftReceiptCase)
        {
            var queueInfo = "DEFAULT";
            var receiptCaseInfo = ftReceiptCase.ToString();

            var signatures = new List<SignaturItem>
            {
                CreateQrCodeSignature("www.fiskaltrust.eu", $"{cashBoxId}_{receiptId}_{receiptHash}_{sumOfPayItems}")
            };

            AddQueueAndReceiptCaseInfo(signatures, queueInfo, receiptCaseInfo);

            return signatures;
        }

        public SignaturItem CreateQrCodeSignature(string caption, string data) => new()
        {
            Caption = caption,
            ftSignatureFormat = (long)SignaturItem.Formats.QR_Code,
            Data = data,
            ftSignatureType = 0x0
        };

        private void AddQueueAndReceiptCaseInfo(List<SignaturItem> signatures, string queueInfo, string receiptCaseInfo)
        {
            foreach (var signature in signatures)
            {
                signature.Caption += $" - Queue Info: {queueInfo}, Receipt Case Info: {receiptCaseInfo}";
            }
        }
    }
}