using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// The receipt QR code every French SCU returns as its <see cref="SignatureTypeFR.ReceiptSignature"/>
/// item, as v1.2 did (type 0x…01, format QR code, caption <c>www.fiskaltrust.fr</c>): the signed
/// content followed by the signature, so a scan alone lets TaxAuditorTool or an auditor re-verify the
/// receipt.
/// <list type="bullet">
/// <item>Infocert: the compact JWS <c>header.payload.signature</c> (ES256, R‖S).</item>
/// <item>LNE, self-certified, in-memory: <c>&lt;data set&gt;.&lt;signature&gt;</c>, the <c>|</c>-delimited
/// data set exactly as signed (UTF-8) and its Base64Url signature.</item>
/// </list>
/// Base64Url has no <c>.</c>, so in both forms the signature is what follows the last <c>.</c>
/// (<see cref="FRChaining.ChainedSignatureOf"/>) and the signed content is what precedes it. The
/// signing certificate's serial number is part of the signed content (<c>csn</c> in the JWS payload,
/// the fourth-from-last field of a data set), as v1.2 carried it; there is no separate certificate item.
/// </summary>
public static class FRReceiptQrCode
{
    /// <summary>The caption v1.2 gave its QR code item.</summary>
    public const string Caption = "www.fiskaltrust.fr";

    /// <summary>The QR content of a delimited data set and its Base64Url signature.</summary>
    public static string OfDataSet(string dataSet, string signature) => $"{dataSet}.{signature}";

    /// <summary>The signed content of a QR code: everything before the last <c>.</c>.</summary>
    public static string SignedContentOf(string qrCode)
    {
        var dot = qrCode.LastIndexOf('.');
        return dot < 0 ? "" : qrCode.Substring(0, dot);
    }

    public static void AddReceiptQrCode(this ReceiptResponse response, string qrCode)
        => response.AddSignatureItem(SignatureTypeFR.ReceiptSignature, Caption, qrCode, SignatureFormat.QRCode);
}
