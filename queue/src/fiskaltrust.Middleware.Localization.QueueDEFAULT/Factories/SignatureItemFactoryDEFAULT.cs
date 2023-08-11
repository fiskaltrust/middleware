using System;
using System.Collections.Generic;
using System.Globalization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Factories;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories
{
    /// <summary>
    /// Class responsible for creating signature items for the DEFAULT country setup.
    /// Note: This implementation is specific to the default queue. In other markets, other
    /// configurations may be required.
    /// For a more advanced example, see <see cref="SignatureItemFactoryIT"/> or refer to
    /// the documentation for the Italian market.
    /// </summary>
    public class SignatureItemFactoryDEFAULT : SignatureItemFactory
    {
        // Represents the base state specific to the country's regulations.
        public override long CountryBaseState => 000000000;

        // Formatter used to format currency values according to the country's standards.
        protected static NumberFormatInfo CurrencyFormatter = new ()
        {
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = "",
            CurrencyDecimalDigits = 2
        };
        // Method to create signatures for a POS receipt.
        public List<SignaturItem> CreatePosReceiptSignatures(Guid cashBoxId, Guid receiptId,
            decimal sumOfPayItems, string previousHash, long ftReceiptCase)
        {
            var queueInfo = "DEFAULT";
            var receiptCaseInfo = ftReceiptCase.ToString();
            
            // Creating a QR code signature along with the included data.
            var signatures = new List<SignaturItem>
            {
                CreateQrCodeSignature("www.fiskaltrust.eu", $"{cashBoxId}_{receiptId}_{sumOfPayItems}_{previousHash}")
            };

            AddQueueAndReceiptCaseInfo(signatures, queueInfo, receiptCaseInfo);

            return signatures;
        }
        // Method to create a QR code signature item with specific caption and data.
        public SignaturItem CreateQrCodeSignature(string caption, string data) => new()
        {
            Caption = caption,
            ftSignatureFormat = (long)SignaturItem.Formats.QR_Code,
            Data = data,
            ftSignatureType = 0x0
        };
        // Private, supportive method to add queue and receipt case information to the signatures.
        private void AddQueueAndReceiptCaseInfo(List<SignaturItem> signatures, string queueInfo, string receiptCaseInfo)
        {
            foreach (var signature in signatures)
            {
                signature.Caption += $" - Queue Info: {queueInfo}, Receipt Case Info: {receiptCaseInfo}";
            }
        }
    }
}