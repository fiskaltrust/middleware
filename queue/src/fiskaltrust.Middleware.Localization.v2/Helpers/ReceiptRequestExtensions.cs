using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models;
using System.Text.Json;

namespace fiskaltrust.Middleware.Localization.v2.Helpers;

public static class ReceiptRequestExtensions
{
    public static List<(ChargeItem chargeItem, List<ChargeItem> modifiers)> GetGroupedChargeItems(this ReceiptRequest receiptRequest)
    {
        var data = new List<(ChargeItem chargeItem, List<ChargeItem> modifiers)>();
        foreach (var receiptChargeItem in receiptRequest.cbChargeItems)
        {
            if (receiptChargeItem.IsVoucherRedeem() || receiptChargeItem.IsDiscountOrExtra())
            {
                var last = data.LastOrDefault();
                if (last == default)
                {
                    data.Add((receiptChargeItem, new List<ChargeItem>()));
                }
                else
                {
                    last.modifiers.Add(receiptChargeItem);
                }
            }
            else
            {
                data.Add((receiptChargeItem, new List<ChargeItem>()));
            }
        }
        return data;
    }

    public static bool ContainsCustomerInfo(this ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbCustomer != null)
        {
            return JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) != null;
        }
        return false;
    }

    public static MiddlewareCustomer? GetCustomerOrNull(this ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbCustomer != null)
        {
            return JsonSerializer.Deserialize<MiddlewareCustomer>(JsonSerializer.Serialize(receiptRequest.cbCustomer), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        return null;
    }

    public static (string? cbPreviousReceiptReferenceString, List<string>? cbPreviousReceiptReferenceArray) GetPreviousReceiptReferenceStringOrArray(this ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbPreviousReceiptReference is JsonElement cbPreviousReceiptReferenceElem)
        {
            if (cbPreviousReceiptReferenceElem.ValueKind == JsonValueKind.String)
            {
                return (cbPreviousReceiptReferenceElem.Deserialize<string>(), null);
            }
            else if (cbPreviousReceiptReferenceElem.ValueKind == JsonValueKind.Array)
            {
                return (null, cbPreviousReceiptReferenceElem.Deserialize<List<string>>());
            }
        }
        if (receiptRequest.cbPreviousReceiptReference is not null && receiptRequest.cbPreviousReceiptReference is string cbPreviousReceiptReferenceString)
        {
            return (cbPreviousReceiptReferenceString, null);
        }
        return (null, null);
    }

    public static bool HasGreeceCountryCode(this ReceiptRequest receiptRequest)
    {
        return ((ulong) receiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000) == 0x4752_0000_0000_0000;
    }

    public static bool HasNonEUCountryCode(this ReceiptRequest receiptRequest)
    {
        return ((ulong) receiptRequest.ftReceiptCase & 0xFFFF_0000_0000_0000) == 0x0000_0000_0000_0000;
    }
}