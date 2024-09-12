using System;
using fiskaltrust.ifPOS.v1;

namespace fiskaltrust.Middleware.Localization.v2
{
    public static class SignatureFactory
    {
        public static SignaturItem CreateSandboxSignature(Guid queueId) =>
            new SignaturItem
            {
                Caption = "S A N D B O X",
                Data = queueId.ToString(),
                ftSignatureFormat = (long) SignaturItem.Formats.Text,
                ftSignatureType = (long) SignaturItem.Types.Unknown
            };
    }
}
