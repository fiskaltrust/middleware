using System.Globalization;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Factories;


namespace fiskaltrust.Middleware.Localization.QueueIT.Factories
{
    public class SignatureItemFactoryIT : SignatureItemFactory
    {
        public override long CountryBaseState => 0x4954000000000000;

        public SignatureItemFactoryIT() { 
        }

        protected static NumberFormatInfo CurrencyFormatter = new ()
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = "",
            CurrencyDecimalDigits = 2
        };

        public SignaturItem []  CreatePosReceiptSignatures(FiscalReceiptResponse fiscalReceiptResponse)
        { 
            return new SignaturItem[]
            {
                new SignaturItem
                {
                    Caption = "<rec-number>",
                    Data = fiscalReceiptResponse.RecNumber.ToString(),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = CountryBaseState & (long) SignaturItem.Types.Information
                },
                new SignaturItem
                {
                    Caption = "<z-number>",
                    Data = fiscalReceiptResponse.ZRecNumber.ToString(),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = CountryBaseState & (long) SignaturItem.Types.Information
                },
                new SignaturItem
                {
                    Caption = "<amount>",
                    Data = fiscalReceiptResponse.Amount.ToString(CurrencyFormatter),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = CountryBaseState & (long) SignaturItem.Types.Information
                },
                new SignaturItem
                {
                    Caption = "<timestamp>",
                    Data = fiscalReceiptResponse.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    ftSignatureFormat = (long) SignaturItem.Formats.Text,
                    ftSignatureType = CountryBaseState & (long) SignaturItem.Types.Information
                }
            };
        }
    }
}
