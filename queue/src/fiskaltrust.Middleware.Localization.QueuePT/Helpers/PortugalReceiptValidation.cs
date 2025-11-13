using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

public static class PortugalReceiptValidation
{
    /// <summary>
    /// Validates that cash payments do not exceed 3000€
    /// </summary>
    /// <param name="request">The receipt request to validate</param>
    /// <returns>Error message if validation fails, null otherwise</returns>
    public static string? ValidateCashPaymentLimit(ReceiptRequest request)
    {
        if (request.cbPayItems == null || request.cbPayItems.Count == 0)
        {
            return null;
        }

        var totalCashAmount = request.cbPayItems
            .Where(payItem => payItem.ftPayItemCase.Case() == PayItemCase.CashPayment)
            .Sum(payItem => payItem.Amount);

        if (totalCashAmount > 3000m)
        {
            return Models.ErrorMessagesPT.EEEE_CashPaymentExceedsLimit;
        }

        return null;
    }

    /// <summary>
    /// Validates that POS receipt net amount does not exceed 1000€
    /// </summary>
    /// <param name="request">The receipt request to validate</param>
    /// <returns>Error message if validation fails, null otherwise</returns>
    public static string? ValidatePosReceiptNetAmountLimit(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            return null;
        }

        var totalNetAmount = request.cbChargeItems
            .Sum(chargeItem => chargeItem.Amount - chargeItem.GetVATAmount());

        if (totalNetAmount > 1000m)
        {
            return Models.ErrorMessagesPT.EEEE_PosReceiptNetAmountExceedsLimit;
        }

        return null;
    }

    /// <summary>
    /// Validates that OtherService charge items do not exceed 100€ net amount
    /// </summary>
    /// <param name="request">The receipt request to validate</param>
    /// <returns>Error message if validation fails, null otherwise</returns>
    public static string? ValidateOtherServiceNetAmountLimit(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            return null;
        }

        var otherServiceNetAmount = request.cbChargeItems
            .Where(chargeItem => chargeItem.ftChargeItemCase.TypeOfService() == ChargeItemCaseTypeOfService.OtherService)
            .Sum(chargeItem => chargeItem.Amount - chargeItem.GetVATAmount());

        if (otherServiceNetAmount > 100m)
        {
            return Models.ErrorMessagesPT.EEEE_OtherServiceNetAmountExceedsLimit;
        }

        return null;
    }
}
