using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.v2.Interface;

public static class SignatureFactory
{
    public static SignatureItem CreateSandboxSignature(Guid queueId) =>
        new SignatureItem
        {
            Caption = "S A N D B O X",
            Data = queueId.ToString(),
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = 0
        };
}
