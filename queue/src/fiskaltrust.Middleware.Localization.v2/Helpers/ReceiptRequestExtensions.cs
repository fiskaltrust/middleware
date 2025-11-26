using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Models;
using System.Text.Json;

namespace fiskaltrust.Middleware.Localization.v2.Helpers;

public static class ReceiptRequestExtensions
{
    public static bool TryDeserializeftReceiptCaseData<T>(this ReceiptRequest request, out T? result) where T : class
    {
        result = default;
        try
        {
            if (request.ftReceiptCaseData is null)
            {
                return false;
            }
            result = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(request.ftReceiptCaseData), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result != null;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }

    public static List<(ChargeItem chargeItem, List<ChargeItem> modifiers)> GetGroupedChargeItemsModifyPositionsIfNotSet(this ReceiptRequest receiptRequest)
    {
        try
        {
            var data = new List<(ChargeItem chargeItem, List<ChargeItem> modifiers)>();
            var currentPos = 1;
            foreach (var receiptChargeItem in receiptRequest.cbChargeItems)
            {
                if (receiptChargeItem.Position == 0)
                {
                    receiptChargeItem.Position = currentPos;
                }
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
                    currentPos++;
                }
            }
            return data;
        }
        catch (Exception ex)
        {
            throw;
        }
    }


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

    public static string? GetcbUserOrNull(this ReceiptRequest receiptRequest)
    {
        if (receiptRequest.cbUser != null)
        {

            if (receiptRequest.cbUser is string userString)
            {
                return userString;
            }

            if (receiptRequest.cbUser is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            {
                return jsonElement.GetString();
            }

            return JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(receiptRequest.cbUser), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        return null;
    }
}