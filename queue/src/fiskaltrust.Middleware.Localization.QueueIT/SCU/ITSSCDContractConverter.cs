using System.Text.Json;
using fiskaltrust.ifPOS.v2.Cases;
using V1 = fiskaltrust.ifPOS.v1;
using V2 = fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueIT.SCU;

/// <summary>
/// Maps between the ifPOS.v2 receipt objects the queue works with and the ifPOS.v1 objects the Italian SCUs
/// (<see cref="fiskaltrust.ifPOS.v1.it.IITSSCD"/>) understand. The v1 model carries the structured members
/// (<c>cbCustomer</c>, <c>ftReceiptCaseData</c>, ...) as JSON strings, which is also how the SCUs parse them.
/// </summary>
public static class ITSSCDContractConverter
{
    public static V1.ReceiptRequest ToV1(V2.ReceiptRequest request) => new()
    {
        ftCashBoxID = request.ftCashBoxID?.ToString(),
        ftQueueID = request.ftQueueID?.ToString(),
        ftPosSystemId = request.ftPosSystemId?.ToString(),
        cbTerminalID = request.cbTerminalID,
        cbReceiptReference = request.cbReceiptReference,
        cbReceiptMoment = request.cbReceiptMoment,
        cbChargeItems = request.cbChargeItems?.Select(ToV1).ToArray() ?? [],
        cbPayItems = request.cbPayItems?.Select(ToV1).ToArray() ?? [],
        ftReceiptCase = (long) request.ftReceiptCase,
        ftReceiptCaseData = ToV1Data(request.ftReceiptCaseData),
        cbReceiptAmount = request.cbReceiptAmount,
        cbUser = ToV1Data(request.cbUser),
        cbArea = ToV1Data(request.cbArea),
        cbCustomer = ToV1Data(request.cbCustomer),
        cbSettlement = ToV1Data(request.cbSettlement),
        cbPreviousReceiptReference = ToV1PreviousReceiptReference(request.cbPreviousReceiptReference),
    };

    public static V1.ChargeItem ToV1(V2.ChargeItem chargeItem) => new()
    {
        Position = (long) chargeItem.Position,
        Quantity = chargeItem.Quantity,
        Description = chargeItem.Description,
        Amount = chargeItem.Amount,
        VATRate = chargeItem.VATRate,
        ftChargeItemCase = (long) chargeItem.ftChargeItemCase,
        ftChargeItemCaseData = ToV1Data(chargeItem.ftChargeItemCaseData),
        VATAmount = chargeItem.VATAmount,
        AccountNumber = chargeItem.AccountNumber,
        CostCenter = chargeItem.CostCenter,
        ProductGroup = chargeItem.ProductGroup,
        ProductNumber = chargeItem.ProductNumber,
        ProductBarcode = chargeItem.ProductBarcode,
        Unit = chargeItem.Unit,
        UnitQuantity = chargeItem.UnitQuantity,
        UnitPrice = chargeItem.UnitPrice,
        Moment = chargeItem.Moment,
    };

    public static V1.PayItem ToV1(V2.PayItem payItem) => new()
    {
        Position = (long) payItem.Position,
        Quantity = payItem.Quantity,
        Description = payItem.Description,
        Amount = payItem.Amount,
        ftPayItemCase = (long) payItem.ftPayItemCase,
        ftPayItemCaseData = ToV1Data(payItem.ftPayItemCaseData),
        AccountNumber = payItem.AccountNumber,
        CostCenter = payItem.CostCenter,
        MoneyGroup = payItem.MoneyGroup,
        MoneyNumber = payItem.MoneyNumber,
        Moment = payItem.Moment,
    };

    public static V1.SignaturItem ToV1(V2.SignatureItem signatureItem) => new()
    {
        ftSignatureFormat = (long) signatureItem.ftSignatureFormat,
        ftSignatureType = (long) signatureItem.ftSignatureType,
        Caption = signatureItem.Caption,
        Data = signatureItem.Data,
    };

    /// <summary>
    /// The response as handed to the SCU. <c>ftStateData</c> is queue-internal (it carries the resolved previous
    /// receipt references) and not part of the SCU contract, so it stays behind and is restored by
    /// <see cref="ToV2(V1.ReceiptResponse, V2.ReceiptResponse)"/>.
    /// </summary>
    public static V1.ReceiptResponse ToV1(V2.ReceiptResponse response) => new()
    {
        ftCashBoxID = response.ftCashBoxID?.ToString(),
        ftQueueID = response.ftQueueID.ToString(),
        ftQueueItemID = response.ftQueueItemID.ToString(),
        ftQueueRow = response.ftQueueRow,
        cbTerminalID = response.cbTerminalID,
        cbReceiptReference = response.cbReceiptReference,
        ftCashBoxIdentification = response.ftCashBoxIdentification,
        ftReceiptIdentification = response.ftReceiptIdentification,
        ftReceiptMoment = response.ftReceiptMoment,
        ftReceiptHeader = response.ftReceiptHeader?.ToArray() ?? [],
        ftChargeItems = response.ftChargeItems?.Select(ToV1).ToArray() ?? [],
        ftChargeLines = response.ftChargeLines?.ToArray() ?? [],
        ftPayItems = response.ftPayItems?.Select(ToV1).ToArray() ?? [],
        ftPayLines = response.ftPayLines?.ToArray() ?? [],
        ftSignatures = response.ftSignatures?.Select(ToV1).ToArray() ?? [],
        ftReceiptFooter = response.ftReceiptFooter?.ToArray() ?? [],
        ftState = (long) response.ftState,
        ftStateData = null,
    };

    /// <summary>
    /// The response as returned by the SCU, back in v2 terms. Identifiers the SCU did not touch (or damaged) fall
    /// back to the <paramref name="original"/>; so does <c>ftStateData</c> unless the SCU reported state of its own.
    /// </summary>
    public static V2.ReceiptResponse ToV2(V1.ReceiptResponse scuResponse, V2.ReceiptResponse original) => new()
    {
        ftCashBoxID = Guid.TryParse(scuResponse.ftCashBoxID, out var cashBoxId) ? cashBoxId : original.ftCashBoxID,
        ftQueueID = Guid.TryParse(scuResponse.ftQueueID, out var queueId) ? queueId : original.ftQueueID,
        ftQueueItemID = Guid.TryParse(scuResponse.ftQueueItemID, out var queueItemId) ? queueItemId : original.ftQueueItemID,
        ftQueueRow = scuResponse.ftQueueRow,
        cbTerminalID = scuResponse.cbTerminalID,
        cbReceiptReference = scuResponse.cbReceiptReference,
        ftCashBoxIdentification = scuResponse.ftCashBoxIdentification,
        ftReceiptIdentification = scuResponse.ftReceiptIdentification,
        ftReceiptMoment = scuResponse.ftReceiptMoment,
        ftReceiptHeader = scuResponse.ftReceiptHeader?.ToList() ?? [],
        ftChargeItems = scuResponse.ftChargeItems?.Select(ToV2).ToList() ?? [],
        ftChargeLines = scuResponse.ftChargeLines?.ToList() ?? [],
        ftPayItems = scuResponse.ftPayItems?.Select(ToV2).ToList() ?? [],
        ftPayLines = scuResponse.ftPayLines?.ToList() ?? [],
        ftSignatures = scuResponse.ftSignatures?.Select(ToV2).ToList() ?? [],
        ftReceiptFooter = scuResponse.ftReceiptFooter?.ToList() ?? [],
        ftState = (State) scuResponse.ftState,
        ftStateData = scuResponse.ftStateData is null ? original.ftStateData : ToV2Data(scuResponse.ftStateData),
    };

    public static V2.ChargeItem ToV2(V1.ChargeItem chargeItem) => new()
    {
        Position = chargeItem.Position,
        Quantity = chargeItem.Quantity,
        Description = chargeItem.Description,
        Amount = chargeItem.Amount,
        VATRate = chargeItem.VATRate,
        ftChargeItemCase = (ChargeItemCase) chargeItem.ftChargeItemCase,
        ftChargeItemCaseData = chargeItem.ftChargeItemCaseData,
        VATAmount = chargeItem.VATAmount,
        AccountNumber = chargeItem.AccountNumber,
        CostCenter = chargeItem.CostCenter,
        ProductGroup = chargeItem.ProductGroup,
        ProductNumber = chargeItem.ProductNumber,
        ProductBarcode = chargeItem.ProductBarcode,
        Unit = chargeItem.Unit,
        UnitQuantity = chargeItem.UnitQuantity,
        UnitPrice = chargeItem.UnitPrice,
        Moment = chargeItem.Moment,
    };

    public static V2.PayItem ToV2(V1.PayItem payItem) => new()
    {
        Position = payItem.Position,
        Quantity = payItem.Quantity,
        Description = payItem.Description,
        Amount = payItem.Amount,
        ftPayItemCase = (PayItemCase) payItem.ftPayItemCase,
        ftPayItemCaseData = payItem.ftPayItemCaseData,
        AccountNumber = payItem.AccountNumber,
        CostCenter = payItem.CostCenter,
        MoneyGroup = payItem.MoneyGroup,
        MoneyNumber = payItem.MoneyNumber,
        Moment = payItem.Moment,
    };

    public static V2.SignatureItem ToV2(V1.SignaturItem signaturItem) => new()
    {
        ftSignatureFormat = (SignatureFormat) signaturItem.ftSignatureFormat,
        ftSignatureType = (SignatureType) signaturItem.ftSignatureType,
        Caption = signaturItem.Caption,
        Data = signaturItem.Data,
    };

    /// <summary>
    /// A v2 "object" member (a string, a JSON element as deserialized from the request, or a POCO) as the JSON
    /// string the v1 model carries. Strings are passed through unchanged.
    /// </summary>
    internal static string? ToV1Data(object? data) => data switch
    {
        null => null,
        string s => s,
        JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement element => element.GetRawText(),
        _ => JsonSerializer.Serialize(data),
    };

    /// <summary>
    /// The state data an SCU reports is JSON; it is exposed as such so that the response serializes it as an
    /// object and not as an escaped string. Anything that is not JSON is kept as the plain string.
    /// </summary>
    internal static object ToV2Data(string data)
    {
        try
        {
            using var document = JsonDocument.Parse(data);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return data;
        }
    }

    /// <summary>
    /// The v1 model knows a single reference only. A group reference cannot be represented; the queue rejects group
    /// references for refunds and voids before the SCU is involved, so this only affects cases the SCU does not read.
    /// </summary>
    internal static string? ToV1PreviousReceiptReference(V2.cbPreviousReceiptReference? previousReceiptReference)
    {
        if (previousReceiptReference is null)
        {
            return null;
        }
        return previousReceiptReference.Match(
            single => single,
            group => group.Length == 1 ? group[0] : null);
    }
}
