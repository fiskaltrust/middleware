using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueueES.Validation.Rules;

public class ReceiptRequestValidations
{
    /// <summary>
    /// Validates that the sum of charge items matches the sum of pay items.
    /// Returns a single ValidationResult if balance validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateReceiptBalance(ReceiptRequest request)
    {
        if(request.ftReceiptCase.IsCase((ReceiptCase) 0x0007) || request.ftReceiptCase.IsCase((ReceiptCase) 0x0006))
        {
            // Delivery notes are not required to be balanced
            yield break;
        }

        var chargeItemsSum = 0m;
        var payItemsSum = 0m;

        if (request.cbChargeItems != null && request.cbChargeItems.Count > 0)
        {
            chargeItemsSum = request.cbChargeItems.Sum(ci => ci.Amount);
        }

        if (request.cbPayItems != null && request.cbPayItems.Count > 0)
        {
            payItemsSum = request.cbPayItems.Sum(pi => pi.Amount);
        }

        const decimal roundingTolerance = 0.01m;
        var difference = Math.Abs(chargeItemsSum - payItemsSum);

        if (difference > roundingTolerance)
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesES.EEEE_ReceiptNotBalanced(chargeItemsSum, payItemsSum, difference),
                "EEEE_ReceiptNotBalanced"
            )
            .WithContext("ChargeItemsSum", chargeItemsSum)
            .WithContext("PayItemsSum", payItemsSum)
            .WithContext("Difference", difference));
        }
    }



    /// <summary>
    /// Validates that refunds have a cbPreviousReceiptReference.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateRefundHasPreviousReference(ReceiptRequest request)
    {
        if (request.cbPreviousReceiptReference is null)
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesES.EEEE_RefundMissingPreviousReceiptReference,
                "EEEE_RefundMissingPreviousReceiptReference",
                "cbPreviousReceiptReference"
            ));
        }
    }
}
