using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules;

public static class GlobalRules
{
    public static IEnumerable<ValidationResult> ChargeItemsMandatoryFields(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];

            // Description validation
            if (string.IsNullOrWhiteSpace(chargeItem.Description))
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessages.EEEE_ChargeItemValidationFailed(i, "description is missing"),
                    "EEEE_ChargeItemDescriptionMissing",
                    "cbChargeItems.Description",
                    i
                ));
            }

            // VAT Rate validation
            if (chargeItem.VATRate < 0)
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessages.EEEE_ChargeItemValidationFailed(i, "VAT rate is missing or invalid"),
                    "EEEE_ChargeItemVATRateMissing",
                    "cbChargeItems.VATRate",
                    i
                ).WithContext("VATRate", chargeItem.VATRate));
            }

            // Amount (price) validation
            if (chargeItem.Amount == 0)
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessages.EEEE_ChargeItemValidationFailed(i, "amount (price) is missing or zero"),
                    "EEEE_ChargeItemAmountMissing",
                    "cbChargeItems.Amount",
                    i
                ).WithContext("Amount", chargeItem.Amount));
            }
        }
    }
}
