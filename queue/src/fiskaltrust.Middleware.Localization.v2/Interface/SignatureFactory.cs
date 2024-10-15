using fiskaltrust.Api.POS.Models.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public static class SignatureFactory
{
    public static SignatureItem CreateSandboxSignature(Guid queueId) =>
        new SignatureItem
        {
            Caption = "S A N D B O X",
            Data = queueId.ToString(),
            ftSignatureFormat = (long) ifPOS.v1.SignaturItem.Formats.Text,
            ftSignatureType = (long) ifPOS.v1.SignaturItem.Types.Unknown
        };
}
