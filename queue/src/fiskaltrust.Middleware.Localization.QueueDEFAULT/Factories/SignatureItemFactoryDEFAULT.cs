using System.Collections.Generic;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories
{
    public class SignatureFactoryDefault
    {
        private const long OPTIONAL_FLAG = 0x10000;
        private readonly QueueDEFAULTConfiguration _queueDefaultConfiguration;

        public SignatureFactoryDefault(QueueDEFAULTConfiguration queueDefaultConfiguration)
        {
            _queueDefaultConfiguration = queueDefaultConfiguration;
        }

        public List<SignaturItem> GetSignaturesForFinishTransaction(FinishTransactionResponse finishTransactionResponse)
        {
            var signatures = new List<SignaturItem>
            {
                CreateQrCodeSignature("www.fiskaltrust.eu", "<data-for-qr-code>", false)
            };

            return signatures;
        }

        public SignaturItem CreateQrCodeSignature(string caption, string data, bool optional) => new SignaturItem
        {
            Caption = caption,
            ftSignatureFormat = AddOptionalFlagIfRequired(SignaturItem.Formats.QR_Code, optional),
            Data = data
        };


        private long AddOptionalFlagIfRequired(SignaturItem.Formats format, bool optional) 
            => _queueDefaultConfiguration.FlagOptionalSignatures && optional ? (long) format | OPTIONAL_FLAG : (long) format;
    }
}
