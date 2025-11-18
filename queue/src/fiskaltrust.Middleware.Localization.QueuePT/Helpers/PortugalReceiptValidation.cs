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

    /// <summary>
    /// Validates that only supported VAT rates are used in charge items.
    /// Portugal supports: DiscountedVatRate1 (RED/6%), DiscountedVatRate2 (INT/13%), NormalVatRate (NOR/23%), and NotTaxable (ISE)
    /// </summary>
    /// <param name="request">The receipt request to validate</param>
    /// <returns>Error message if validation fails, null otherwise</returns>
    public static string? ValidateSupportedVatRates(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            return null;
        }

        var unsupportedVatRates = new[]
        {
            ChargeItemCase.UnknownService,
            ChargeItemCase.SuperReducedVatRate1,
            ChargeItemCase.SuperReducedVatRate2,
            ChargeItemCase.ParkingVatRate,
            ChargeItemCase.ZeroVatRate
        };

        for (int i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];
            var vatRate = chargeItem.ftChargeItemCase.Vat();

            if (unsupportedVatRates.Contains(vatRate))
            {
                return Models.ErrorMessagesPT.EEEE_UnsupportedVatRate(i, vatRate);
            }
        }

        return null;
    }

    /// <summary>
    /// Validates that the VAT rate category matches the specified VAT rate percentage,
    /// and that the VAT amount is correctly calculated.
    /// Considers rounding differences up to 0.01 (1 cent) per item.
    /// </summary>
    /// <param name="request">The receipt request to validate</param>
    /// <returns>Error message if validation fails, null otherwise</returns>
    public static string? ValidateVatRateAndAmount(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            return null;
        }

        // Define expected VAT rates for each category in Portugal (mainland)
        var expectedVatRates = new Dictionary<ChargeItemCase, decimal>
        {
            { ChargeItemCase.DiscountedVatRate1, 6.0m },      // RED
            { ChargeItemCase.DiscountedVatRate2, 13.0m },     // INT
            { ChargeItemCase.NormalVatRate, 23.0m },          // NOR
            { ChargeItemCase.NotTaxable, 0.0m }               // ISE
        };

        const decimal roundingTolerance = 0.01m; // 1 cent tolerance for rounding differences

        for (int i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];
            var vatRateCategory = chargeItem.ftChargeItemCase.Vat();

            // Skip unsupported VAT rates (they should be caught by ValidateSupportedVatRates)
            if (!expectedVatRates.ContainsKey(vatRateCategory))
            {
                continue;
            }

            var expectedVatRatePercentage = expectedVatRates[vatRateCategory];

            // Check if the VAT rate percentage matches the category
            if (Math.Abs(chargeItem.VATRate - expectedVatRatePercentage) > 0.001m)
            {
                return Models.ErrorMessagesPT.EEEE_VatRateMismatch(i, vatRateCategory, expectedVatRatePercentage, chargeItem.VATRate);
            }

            // Validate VAT amount calculation
            if (chargeItem.VATAmount.HasValue && chargeItem.VATRate > 0)
            {
                // Calculate expected VAT amount: VATAmount = Amount / (100 + VATRate) * VATRate
                var calculatedVatAmount = chargeItem.Amount / (100 + chargeItem.VATRate) * chargeItem.VATRate;
                var difference = Math.Abs(chargeItem.VATAmount.Value - calculatedVatAmount);

                if (difference > roundingTolerance)
                {
                    return Models.ErrorMessagesPT.EEEE_VatAmountMismatch(i, chargeItem.VATAmount.Value, calculatedVatAmount, difference);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Validates that non-refund receipts do not have negative quantities or amounts,
    /// except for discounts which are allowed to be negative.
    /// </summary>
    /// <param name="request">The receipt request to validate</param>
    /// <param name="isRefund">Whether this is a refund receipt</param>
    /// <returns>Error message if validation fails, null otherwise</returns>
    public static string? ValidateNegativeAmountsAndQuantities(ReceiptRequest request, bool isRefund)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            return null;
        }

        // Skip validation for refund receipts
        if (isRefund)
        {
            return null;
        }

        for (int i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];

            // Discounts are allowed to be negative
            if (chargeItem.IsDiscount())
            {
                continue;
            }

            // Check for negative quantity
            if (chargeItem.Quantity < 0)
            {
                return Models.ErrorMessagesPT.EEEE_NegativeQuantityNotAllowed(i, chargeItem.Quantity);
            }

            // Check for negative amount
            if (chargeItem.Amount < 0)
            {
                return Models.ErrorMessagesPT.EEEE_NegativeAmountNotAllowed(i, chargeItem.Amount);
            }
        }

        return null;
    }

    /// <summary>
    /// Validates that the sum of charge items matches the sum of pay items.
    /// This ensures the receipt balances correctly from an accounting perspective.
    /// Considers rounding differences up to 0.01 (1 cent).
    /// </summary>
    /// <param name="request">The receipt request to validate</param>
    /// <returns>Error message if validation fails, null otherwise</returns>
    public static string? ValidateReceiptBalance(ReceiptRequest request)
    {
        decimal chargeItemsSum = 0m;
        decimal payItemsSum = 0m;

        if (request.cbChargeItems != null && request.cbChargeItems.Count > 0)
        {
            chargeItemsSum = request.cbChargeItems.Sum(ci => ci.Amount);
        }

        if (request.cbPayItems != null && request.cbPayItems.Count > 0)
        {
            payItemsSum = request.cbPayItems.Sum(pi => pi.Amount);
        }

        const decimal roundingTolerance = 0.01m; // 1 cent tolerance for rounding differences
        var difference = Math.Abs(chargeItemsSum - payItemsSum);

        if (difference > roundingTolerance)
        {
            return Models.ErrorMessagesPT.EEEE_ReceiptNotBalanced(chargeItemsSum, payItemsSum, difference);
        }

        return null;
    }

    /// <summary>
    /// Validates that cbUser follows the required PTUserObject structure for Portugal.
    /// PTUserObject must have UserId, and optionally UserDisplayName and UserEmail.
    /// </summary>
    /// <param name="request">The receipt request to validate</param>
    /// <returns>Error message if validation fails, null otherwise</returns>
    public static string? ValidateUserStructure(ReceiptRequest request)
    {
        if (request.cbUser == null)
        {
            // cbUser is optional, so null is valid
            return null;
        }

        try
        {
            // Attempt to deserialize cbUser to PTUserObject
            var userJson = System.Text.Json.JsonSerializer.Serialize(request.cbUser);
            var userObject = System.Text.Json.JsonSerializer.Deserialize<Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.PTUserObject>(userJson);

            if (userObject == null)
            {
                return Models.ErrorMessagesPT.EEEE_InvalidUserStructure("cbUser could not be deserialized to PTUserObject structure.");
            }

            // Validate that at least UserId is provided
            if (string.IsNullOrWhiteSpace(userObject.UserId))
            {
                return Models.ErrorMessagesPT.EEEE_InvalidUserStructure("cbUser must contain a non-empty 'UserId' property.");
            }

            return null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            return Models.ErrorMessagesPT.EEEE_InvalidUserStructure($"cbUser format is invalid: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates that cbUser is present for receipts that generate signatures.
    /// In Portugal, all receipts that generate signatures must have a user identified.
    /// </summary>
    /// <param name="request">The receipt request to validate</param>
    /// <param name="generatesSignature">Whether this receipt type generates a signature</param>
    /// <returns>Error message if validation fails, null otherwise</returns>
    public static string? ValidateUserPresenceForSignatures(ReceiptRequest request, bool generatesSignature)
    {
        // Only validate if this receipt generates a signature
        if (!generatesSignature)
        {
            return null;
        }

        // cbUser must be present for receipts that generate signatures
        if (request.cbUser == null)
        {
            return Models.ErrorMessagesPT.EEEE_UserRequiredForSignatures;
        }

        return null;
    }
}
