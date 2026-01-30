using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules;

public static class ESRules
{

    public static IEnumerable<ValidationResult> ChargeItemsVATAmountRequired(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];

            if (!chargeItem.VATAmount.HasValue)
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessages.EEEE_ChargeItemValidationFailed(i, "VAT amount is missing"),
                    "EEEE_ChargeItemVATAmountMissing",
                    "cbChargeItems.VATAmount",
                    i
                ));
            }
        }
    }
}
