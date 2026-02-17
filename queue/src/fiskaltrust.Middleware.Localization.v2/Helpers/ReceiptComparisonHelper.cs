using System.Text.Json;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Helpers;

public static class ReceiptComparisonHelper
{
    public static string? CompareReceiptRequest(ReceiptRequest refundRequest, ReceiptRequest originalRequest)
    {
        if (originalRequest.ftCashBoxID != refundRequest.ftCashBoxID)
        {
            return "ftCashBoxID mismatch";
        }

        var originalCase = ((long)originalRequest.ftReceiptCase) & 0x0000_0000_0000_FFFF;
        var refundCase = ((long)refundRequest.ftReceiptCase) & 0x0000_0000_0000_FFFF;
        if (originalCase != refundCase)
        {
            return "ftReceiptCase mismatch";
        }

        if (originalRequest.ftReceiptCaseData != refundRequest.ftReceiptCaseData)
        {
            return "ftReceiptCaseData mismatch";
        }

        if (originalRequest.cbArea != refundRequest.cbArea)
        {
            return "cbArea mismatch";
        }

        if (originalRequest.cbCustomer is string originalCustomer && refundRequest.cbCustomer is string refundCustomer)
        {
            if (originalCustomer != refundCustomer)
            {
                return "cbCustomer mismatch";
            }
        }

        if (originalRequest.cbCustomer is JsonElement originalJsonCustomer && refundRequest.cbCustomer is JsonElement refundJsonCustomer)
        {
            if (!JsonSerializer.Serialize(originalJsonCustomer).Equals(JsonSerializer.Serialize(refundJsonCustomer), StringComparison.Ordinal))
            {
                return "cbCustomer mismatch";
            }
        }

        if (originalRequest.cbSettlement != refundRequest.cbSettlement)
        {
            return "cbSettlement mismatch";
        }

        if (originalRequest.Currency != refundRequest.Currency)
        {
            return "Currency mismatch";
        }

        if (originalRequest.DecimalPrecisionMultiplier != refundRequest.DecimalPrecisionMultiplier)
        {
            return "DecimalPrecisionMultiplier mismatch";
        }

        return null;
    }

    public static string? CompareChargeItems(ChargeItem refundItem, ChargeItem originalItem)
    {
        if (Math.Abs(originalItem.Quantity - Math.Abs(refundItem.Quantity)) > 0.001m)
        {
            return "Quantity mismatch";
        }

        if (originalItem.Description != refundItem.Description)
        {
            return "Description mismatch";
        }

        if (Math.Abs(originalItem.Amount - Math.Abs(refundItem.Amount)) > 0.01m)
        {
            return "Amount mismatch";
        }

        if (Math.Abs(originalItem.VATRate - refundItem.VATRate) > 0.001m)
        {
            return "VATRate mismatch";
        }

        var originalCase = ((long)originalItem.ftChargeItemCase) & 0x0000_0000_0000_FFFF;
        var refundCase = ((long)refundItem.ftChargeItemCase) & 0x0000_0000_0000_FFFF;
        if (originalCase != refundCase)
        {
            return "ftChargeItemCase mismatch";
        }

        if (originalItem.ftChargeItemCaseData != refundItem.ftChargeItemCaseData)
        {
            return "ftChargeItemCaseData mismatch";
        }

        if (Math.Abs(originalItem.GetVATAmount() - Math.Abs(refundItem.GetVATAmount())) > 0.001m)
        {
            return "VATAmount mismatch";
        }

        if (originalItem.Position != refundItem.Position)
        {
            return "Position mismatch";
        }

        if (originalItem.AccountNumber != refundItem.AccountNumber)
        {
            return "AccountNumber mismatch";
        }

        if (originalItem.CostCenter != refundItem.CostCenter)
        {
            return "CostCenter mismatch";
        }

        if (originalItem.ProductGroup != refundItem.ProductGroup)
        {
            return "ProductGroup mismatch";
        }

        if (originalItem.ProductNumber != refundItem.ProductNumber)
        {
            return "ProductNumber mismatch";
        }

        if (originalItem.ProductBarcode != refundItem.ProductBarcode)
        {
            return "ProductBarcode mismatch";
        }

        if (originalItem.Unit != refundItem.Unit)
        {
            return "Unit mismatch";
        }

        if (Math.Abs(originalItem.UnitQuantity ?? 0.0m - Math.Abs(refundItem.UnitQuantity ?? 0.0m)) > 0.001m)
        {
            return "UnitQuantity mismatch";
        }

        if (Math.Abs(originalItem.UnitPrice ?? 0.0m - Math.Abs(refundItem.UnitPrice ?? 0.0m)) > 0.001m)
        {
            return "UnitPrice mismatch";
        }

        if (originalItem.Currency != refundItem.Currency)
        {
            return "Currency mismatch";
        }

        if (originalItem.DecimalPrecisionMultiplier != refundItem.DecimalPrecisionMultiplier)
        {
            return "DecimalPrecisionMultiplier mismatch";
        }

        return null;
    }

    public static string? ComparePayItems(PayItem refundItem, PayItem originalItem)
    {
        if (Math.Abs(originalItem.Quantity - Math.Abs(refundItem.Quantity)) > 0.001m)
        {
            return "Quantity mismatch";
        }

        if (originalItem.Description != refundItem.Description)
        {
            return "Description mismatch";
        }

        if (Math.Abs(originalItem.Amount - Math.Abs(refundItem.Amount)) > 0.01m)
        {
            return "Amount mismatch";
        }

        var originalCase = ((long)originalItem.ftPayItemCase) & 0x0000_0000_0000_FFFF;
        var refundCase = ((long)refundItem.ftPayItemCase) & 0x0000_0000_0000_FFFF;
        if (originalCase != refundCase)
        {
            return "ftPayItemCase mismatch";
        }

        if (originalItem.ftPayItemCaseData != refundItem.ftPayItemCaseData)
        {
            return "ftPayItemCaseData mismatch";
        }

        if (originalItem.Position != refundItem.Position)
        {
            return "Position mismatch";
        }

        if (originalItem.AccountNumber != refundItem.AccountNumber)
        {
            return "AccountNumber mismatch";
        }

        if (originalItem.CostCenter != refundItem.CostCenter)
        {
            return "CostCenter mismatch";
        }

        if (originalItem.MoneyGroup != refundItem.MoneyGroup)
        {
            return "MoneyGroup mismatch";
        }

        if (originalItem.MoneyNumber != refundItem.MoneyNumber)
        {
            return "MoneyNumber mismatch";
        }

        if (originalItem.MoneyBarcode != refundItem.MoneyBarcode)
        {
            return "MoneyBarcode mismatch";
        }

        if (originalItem.Currency != refundItem.Currency)
        {
            return "Currency mismatch";
        }

        if (originalItem.DecimalPrecisionMultiplier != refundItem.DecimalPrecisionMultiplier)
        {
            return "DecimalPrecisionMultiplier mismatch";
        }

        return null;
    }
}
