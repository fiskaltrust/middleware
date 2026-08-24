using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;

public static class SignaturItemFactory
{
    public static SignatureItem CreateInitialOperationSignature(ftQueue queue) => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.InitialOperationReceipt.As<SignatureType>(),
        Caption = "Initial-operation receipt",
        Data = $"Queue-ID: {queue.ftQueueId}",
    };

    public static SignatureItem CreateOutOfOperationSignature(ftQueue queue) => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.OutOfOperationReceipt.As<SignatureType>(),
        Caption = "Out-of-operation receipt",
        Data = $"Queue-ID: {queue.ftQueueId}",
    };

    public static SignatureItem CreateStoredNotSignedSignature() => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.StoredNotSigned.As<SignatureType>(),
        Caption = "Stored, not signed",
        Data = "The receipt was persisted by the middleware but not signed. Configure an SCU.FR.InfoCert or SCU.FR.LNE to sign it.",
    };

    /// <summary>
    /// The mention the customer copy has to carry, so a duplicate can never be mistaken for the
    /// original document.
    /// </summary>
    public static SignatureItem CreateDuplicateSignature(string? copiedReceiptReference) => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.Information.As<SignatureType>(),
        Caption = "Duplicata",
        Data = $"Duplicata de {copiedReceiptReference}",
    };

    /// <summary>The mention a bill, pro forma or delivery note has to carry: it is not a receipt.</summary>
    public static SignatureItem CreateProvisionalDocumentSignature() => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.Information.As<SignatureType>(),
        Caption = "Document provisoire",
        Data = "Ce document ne constitue pas une facture.",
    };
}
