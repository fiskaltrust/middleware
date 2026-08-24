using System;
using System.Text.Json.Serialization;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

/// <summary>
/// The json data set the Infocert signature covers. It carries the receipt identity, the totals
/// broken down by VAT class and payment nature, and the hash of the previous entry of the same
/// chain — the elements NF525 requires for inaltérabilité.
/// </summary>
/// <remarks>
/// The field set has to be confirmed against the Infocert dossier before certification; the
/// layout follows the payload the v1 QueueFR localization signs, so existing French archives
/// stay readable with the same tooling.
/// </remarks>
internal class InfoCertReceiptPayload
{
    [JsonPropertyName("queueId")] public Guid QueueId { get; set; }

    [JsonPropertyName("queueItemId")] public Guid QueueItemId { get; set; }

    [JsonPropertyName("cashBoxIdentification")] public string? CashBoxIdentification { get; set; }

    [JsonPropertyName("siret")] public string Siret { get; set; } = "";

    [JsonPropertyName("receiptId")] public string? ReceiptId { get; set; }

    [JsonPropertyName("receiptMoment")] public DateTime ReceiptMoment { get; set; }

    [JsonPropertyName("receiptCase")] public long ReceiptCase { get; set; }

    [JsonPropertyName("currency")] public string Currency { get; set; } = "EUR";

    [JsonPropertyName("totalizer")] public decimal Totalizer { get; set; }

    [JsonPropertyName("ciNormal")] public decimal CINormal { get; set; }

    [JsonPropertyName("ciReduced1")] public decimal CIReduced1 { get; set; }

    [JsonPropertyName("ciReduced2")] public decimal CIReduced2 { get; set; }

    [JsonPropertyName("ciReducedS")] public decimal CIReducedS { get; set; }

    [JsonPropertyName("ciZero")] public decimal CIZero { get; set; }

    [JsonPropertyName("ciUnknown")] public decimal CIUnknown { get; set; }

    [JsonPropertyName("piCash")] public decimal PICash { get; set; }

    [JsonPropertyName("piNonCash")] public decimal PINonCash { get; set; }

    [JsonPropertyName("piInternal")] public decimal PIInternal { get; set; }

    [JsonPropertyName("piUnknown")] public decimal PIUnknown { get; set; }

    [JsonPropertyName("lastHash")] public string LastHash { get; set; } = "";

    [JsonPropertyName("certificateSerialNumber")] public string CertificateSerialNumber { get; set; } = "";

    [JsonPropertyName("attestationNumber")] public string AttestationNumber { get; set; } = "";
}
