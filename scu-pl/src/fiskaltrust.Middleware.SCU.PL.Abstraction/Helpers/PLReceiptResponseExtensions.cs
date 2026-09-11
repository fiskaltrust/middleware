using System.Globalization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Models;

namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;

/// <summary>
/// The single place where PL SCUs enrich a ReceiptResponse with device-owned identity:
/// ftCashBoxIdentification carries the numer unikatowy and ftReceiptIdentification is completed
/// with the fiscal document number issued by the register.
/// </summary>
public static class PLReceiptResponseExtensions
{
    public static void AddSignatureItem(this ReceiptResponse response, SignatureTypePL signatureType, string caption, string data)
        => response.ftSignatures.Add(new SignatureItem
        {
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = (SignatureType)(ulong)(long)signatureType,
            Caption = caption,
            Data = data,
        });

    public static void EnrichWithDeviceIdentification(this ReceiptResponse response, PLDeviceInfo deviceInfo)
    {
        if (!string.IsNullOrEmpty(deviceInfo.UniqueDeviceNumber))
        {
            response.ftCashBoxIdentification = deviceInfo.UniqueDeviceNumber;
            response.AddSignatureItem(SignatureTypePL.UniqueDeviceNumber, "Numer unikatowy", deviceInfo.UniqueDeviceNumber);
        }

        if (!string.IsNullOrEmpty(deviceInfo.DeviceSerialNumber))
        {
            response.AddSignatureItem(SignatureTypePL.DeviceSerialNumber, "Numer fabryczny", deviceInfo.DeviceSerialNumber);
        }
    }

    /// <summary>
    /// The unique eDokument id (<c>ha</c>) the register assigned when the document was bound to an
    /// e-receipt customer identifier — the handle for any later buffer/delivery lookup.
    /// </summary>
    public static void EnrichWithEDocumentId(this ReceiptResponse response, uint eDocumentId)
        => response.AddSignatureItem(SignatureTypePL.EDocumentId, "Identyfikator eDokumentu", eDocumentId.ToString(CultureInfo.InvariantCulture));

    public static void EnrichWithFiscalDocumentNumber(this ReceiptResponse response, long fiscalDocumentNumber)
    {
        var number = fiscalDocumentNumber.ToString(CultureInfo.InvariantCulture);
        response.ftReceiptIdentification += number;
        response.AddSignatureItem(SignatureTypePL.FiscalDocumentNumber, "Numer dokumentu fiskalnego", number);
    }
}
