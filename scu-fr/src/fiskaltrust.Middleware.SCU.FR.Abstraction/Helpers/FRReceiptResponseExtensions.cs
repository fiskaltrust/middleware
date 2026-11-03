using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
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

    /// <summary>
    /// Reports a receipt the SCU could not sign. An SCU never throws to report a failure: it may run
    /// in the queue's process or behind REST (signing.fiskaltrust.fr), and only the
    /// <see cref="ProcessResponse"/> reaches the queue the same way in both. The receipt is unsigned,
    /// so it carries <see cref="StateFR.SigningUnavailableError"/>, the reason in a FAILURE item and
    /// no chain signature.
    /// </summary>
    public static (ProcessResponse response, string signature) ToSigningFailure(this ReceiptResponse response, string reason)
    {
        response.ftState = (State) StateFR.SigningUnavailableError;
        response.ftSignatures = new List<SignatureItem>
        {
            new()
            {
                ftSignatureFormat = SignatureFormat.Text,
                ftSignatureType = ((SignatureType) (ulong) (long) SignatureTypeFR.Information).WithCategory(SignatureTypeCategory.Failure),
                Caption = "FAILURE",
                Data = reason,
            },
        };

        return (new ProcessResponse { ReceiptResponse = response }, "");
    }
}
