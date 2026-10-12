using System;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

/// <summary>
/// The json data set the Infocert signature covers for a technical event (JET, market-fr#200):
/// the fields of the R19 v4.2 "Signature" sheet, in its order, plus the chain counter of ADR 0001.
/// <list type="table">
/// <item><term>queueItemId</term><description>TAG-JET-NID (JET-NID = ftQueueItemID)</description></item>
/// <item><term>eventCode</term><description>TAG-JET-COD</description></item>
/// <item><term>information</term><description>TAG-JET-LIB - the sheet signs the contextual information (JET-INF), not the label; empty when absent</description></item>
/// <item><term>receiptMoment</term><description>TAG-JET-HOR-GDH</description></item>
/// <item><term>operatorId</term><description>TAG-JET-OPS-NID (cbUser; empty when absent)</description></item>
/// <item><term>cashBoxIdentification</term><description>TAG-JET-CAI-NID</description></item>
/// <item><term>previousSignatureCarriedOver</term><description>TAG-JET-OEN, O/N (<see cref="FRChaining.Flag"/>)</description></item>
/// <item><term>previousSignature</term><description>TAG-JET-SIG, Base64Url</description></item>
/// </list>
/// </summary>
/// <remarks>
/// R19 fixes which fields are signed and in which order, not how they are serialized. The json key
/// names follow the camelCase convention of <see cref="InfoCertReceiptPayload"/> and are not an
/// Infocert format; they have to be confirmed with the Infocert reference example (market-fr#108).
/// </remarks>
internal class InfoCertJetPayload
{
    [JsonPropertyName("queueItemId")] public Guid QueueItemId { get; set; }

    [JsonPropertyName("eventCode")] public int EventCode { get; set; }

    [JsonPropertyName("information")] public string Information { get; set; } = "";

    [JsonPropertyName("receiptMoment")] public DateTime ReceiptMoment { get; set; }

    [JsonPropertyName("operatorId")] public string OperatorId { get; set; } = "";

    [JsonPropertyName("cashBoxIdentification")] public string CashBoxIdentification { get; set; } = "";

    [JsonPropertyName("previousSignatureCarriedOver")] public string PreviousSignatureCarriedOver { get; set; } = FRChaining.StartOfChain;

    [JsonPropertyName("previousSignature")] public string PreviousSignature { get; set; } = "";

    [JsonPropertyName("chainCounter")] public long ChainCounter { get; set; }

    /// <summary>The JET payload of <paramref name="request"/>, or null when it is not a valid JET event.</summary>
    public static InfoCertJetPayload? TryCreate(ProcessRequest request, string? previousSignature)
    {
        if (!FRJetEvent.IsJetEvent(request.ReceiptRequest) || !FRJetEvent.TryRead(request.ReceiptRequest, out var jetEvent, out _))
        {
            return null;
        }

        var response = request.ReceiptResponse;
        return new InfoCertJetPayload
        {
            QueueItemId = response.ftQueueItemID,
            EventCode = jetEvent!.Code,
            Information = jetEvent.Information ?? "",
            ReceiptMoment = response.ftReceiptMoment,
            OperatorId = jetEvent.Operator ?? "",
            CashBoxIdentification = response.ftCashBoxIdentification ?? "",
            PreviousSignatureCarriedOver = FRChaining.Flag(previousSignature),
            PreviousSignature = previousSignature ?? "",
            ChainCounter = request.ChainCounter,
        };
    }
}
