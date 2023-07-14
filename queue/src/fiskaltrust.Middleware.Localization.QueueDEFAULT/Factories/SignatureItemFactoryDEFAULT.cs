using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories
{
    public class SignatureFactoryDefault
    {
        public List<SignaturItem> GetSignaturesForPosReceiptTransaction()
        {
            var signatures = new List<SignaturItem>
            {
                CreateQrCodeSignature("www.fiskaltrust.eu", "<data-for-qr-code>")
            };

            return signatures;
        }

        public SignaturItem CreateQrCodeSignature(string caption, string data) => new()
        {
            Caption = caption,
            ftSignatureFormat = (long)SignaturItem.Formats.QR_Code,
            Data = data
        };
        
    }
}
