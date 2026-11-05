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
    /// original document, with its reprint number (R13 ENC-DUP-PRN-NUM, printed on every document
    /// type; market-fr#235). Same wording as v1.2.
    /// </summary>
    /// <param name="copyNumber">The reprint number; null for a training copy, which is not counted.</param>
    public static SignatureItem CreateDuplicateSignature(string copiedReceiptReference, int? copyNumber) => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.Information.As<SignatureType>(),
        Caption = "Duplicata",
        Data = copyNumber is null ? $"Duplicata de {copiedReceiptReference}" : $"{copyNumber}. Duplicata de {copiedReceiptReference}",
    };

    /// <summary>The mention v1.2 prints on every payment proof (workerFR <c>PaymentProveRequest</c>).</summary>
    public static SignatureItem CreatePaymentProofSignature() => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.Information.As<SignatureType>(),
        Caption = "Justificatif de paiement",
        Data = "Justificatif non valable pour encaissement",
    };

    /// <summary>The mention a bill, pro forma or order has to carry: it is not a receipt.</summary>
    public static SignatureItem CreateProvisionalDocumentSignature() => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.Information.As<SignatureType>(),
        Caption = "Document provisoire",
        Data = "Ce document ne constitue pas une facture.",
    };

    /// <summary>
    /// The mention every document of the training chain has to carry, so a fictitious sale can never
    /// be mistaken for a real one: NF525 V2.3 §6.12.1.3 ("NON VALABLE POUR ENCAISSEMENT", "FACTICE",
    /// "SIMULATION", "TEST" or another clear mention) and LNE Exigence 5 ("factice", "simulation").
    /// The POS prints it at least as large as the rest of the document.
    /// </summary>
    public static SignatureItem CreateTrainingSignature() => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.Information.As<SignatureType>(),
        Caption = "Mode école",
        Data = "SIMULATION - NON VALABLE POUR ENCAISSEMENT",
    };
}
