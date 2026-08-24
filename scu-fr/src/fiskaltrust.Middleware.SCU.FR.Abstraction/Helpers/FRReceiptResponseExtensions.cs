using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// Response enrichment shared by the French SCUs. This is presentation only — how a signature is
/// produced is each SCU's own business, only how it is attached to the response is common.
/// </summary>
public static class FRReceiptResponseExtensions
{
    public static void AddSignatureItem(this ReceiptResponse response, SignatureTypeFR signatureType, string caption, string data, SignatureFormat format = SignatureFormat.Text)
        => response.ftSignatures.Add(new SignatureItem
        {
            ftSignatureFormat = format,
            ftSignatureType = (SignatureType) (ulong) (long) signatureType,
            Caption = caption,
            Data = data,
        });
}
