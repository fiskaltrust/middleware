using System;
using System.Globalization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
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

    /// <summary>
    /// Turns an SCU failure into the error response the SCU returns — a Polish SCU never throws out
    /// of ProcessReceiptAsync, so a queue (in-process or remote) reads the outcome off the response
    /// alone. The ftState is the plain error for now; the FAILURE signature says what to do next,
    /// because the remedy differs: an unreachable register is retried, an unknown outcome is checked
    /// on the register first (a document may have been printed, a resend would duplicate it).
    /// </summary>
    public static void SetError(this ReceiptResponse response, Exception exception)
    {
        response.ftState = response.ftState.WithState(State.Error);
        response.ftSignatures = [];
        response.ftSignatures.Add(new SignatureItem
        {
            Caption = "FAILURE",
            Data = DescribeFailure(exception),
            ftSignatureFormat = SignatureFormat.Text,
            ftSignatureType = response.ftState.Reset().As<SignatureType>().WithCategory(SignatureTypeCategory.Failure),
        });
    }

    private static string DescribeFailure(Exception exception) => exception switch
    {
        PLDeviceOutcomeUnknownException => $"The Polish fiscal register did not confirm the command — it may or may not have executed it, so a receipt or report may already have been printed. Do NOT resend blindly: check the register's last document first, and resend only if it was not issued — a resend would otherwise duplicate it. Details: {exception.Message}",
        PLDeviceUnreachableException => $"The Polish fiscal register could not be reached — without a working register no sale may legally be recorded (Art. 111(3) VAT Act). Retry once the register (or its reserve) is available. Details: {exception.Message}",
        _ => exception.Message,
    };

    public static void EnrichWithFiscalDocumentNumber(this ReceiptResponse response, long fiscalDocumentNumber)
    {
        var number = fiscalDocumentNumber.ToString(CultureInfo.InvariantCulture);
        response.ftReceiptIdentification += number;
        response.AddSignatureItem(SignatureTypePL.FiscalDocumentNumber, "Numer dokumentu fiskalnego", number);
    }
}
