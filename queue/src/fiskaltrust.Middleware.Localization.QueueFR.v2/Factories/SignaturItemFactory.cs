using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
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

    /// <summary>
    /// Printed on the zero receipt that ends the late-signing mode: the range of receipts the POS
    /// issued while the middleware was unreachable (v1.2 "Failure registered", market-fr#231).
    /// </summary>
    public static SignatureItem CreateLateSigningEndedSignature(string? firstReceiptIdentification, string? closingReceiptIdentification) => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.Information.As<SignatureType>(),
        Caption = "Défaillance enregistrée",
        Data = $"du {firstReceiptIdentification} au {closingReceiptIdentification}",
    };

    private static readonly JsonSerializerOptions _jetJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Tells the POS which JET event (R19) to send: it copies <see cref="SignatureItem.Data"/>
    /// unchanged into the ftReceiptCaseData of a <c>ProtocolTechnicalEvent 0x3001</c>, with
    /// cbPreviousReceiptReference set to the receipt that carried this item. The queue issues no
    /// JET entry itself (market-fr#231, #208).
    /// </summary>
    public static SignatureItem CreateJetEventSignature(int code, string message, string? information) => new()
    {
        ftSignatureFormat = SignatureFormat.Text,
        ftSignatureType = SignatureTypeFR.JetEvent.As<SignatureType>().WithFlag(SignatureTypeFlags.VisualizationOptional),
        Caption = "JET",
        Data = JsonSerializer.Serialize(new JetEventData(code.ToString(CultureInfo.InvariantCulture), message, information), _jetJson),
    };

    private sealed record JetEventData(string Code, string Message, string? Information);

    /// <summary>The mention a bill, pro forma or delivery note has to carry: it is not a receipt.</summary>
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
