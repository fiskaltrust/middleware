using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;

public class ReceiptRequestValidations
{
    /// <summary>
    /// Validates that the sum of charge items matches the sum of pay items.
    /// Returns a single ValidationResult if balance validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateReceiptBalance(ReceiptRequest request)
    {
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
                ErrorMessagesPT.EEEE_ReceiptNotBalanced(chargeItemsSum, payItemsSum, difference),
                "EEEE_ReceiptNotBalanced"
            )
            .WithContext("ChargeItemsSum", chargeItemsSum)
            .WithContext("PayItemsSum", payItemsSum)
            .WithContext("Difference", difference));
        }
    }

    /// <summary>
    /// Validates that OtherService charge items do not exceed 100€ net amount.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateOtherServiceNetAmountLimit(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        var otherServiceNetAmount = request.cbChargeItems
            .Where(chargeItem => chargeItem.ftChargeItemCase.TypeOfService() == ChargeItemCaseTypeOfService.OtherService)
            .Sum(chargeItem => chargeItem.Amount - chargeItem.GetVATAmount());

        if (otherServiceNetAmount > 100m)
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_OtherServiceNetAmountExceedsLimit,
                "EEEE_OtherServiceNetAmountExceedsLimit",
                "cbChargeItems"
            ).WithContext("OtherServiceNetAmount", otherServiceNetAmount)
             .WithContext("Limit", 100m));
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
                ErrorMessagesPT.EEEE_RefundMissingPreviousReceiptReference,
                "EEEE_RefundMissingPreviousReceiptReference",
                "cbPreviousReceiptReference"
            ));
        }
    }

    /// <summary>
    /// Validates that receipt moment is not more than 10 minutes different from the current server time,
    /// and that it is never in the future.
    /// This ensures receipts are created with accurate timestamps (except for handwritten receipts which may be backdated).
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateReceiptMomentOrder(ReceiptRequest request, object series, bool isHandwritten)
    {
        var serverTime = DateTime.UtcNow;
        var receiptMomentUtc = request.cbReceiptMoment.ToUniversalTime();

        // Check if receipt moment is in the future - this is always invalid
        if (receiptMomentUtc > serverTime)
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_CbReceiptMomentInFuture(request.cbReceiptMoment, serverTime),
                "EEEE_CbReceiptMomentInFuture",
                "cbReceiptMoment"
            )
            .WithContext("ServerTime", serverTime)
            .WithContext("CbReceiptMoment", request.cbReceiptMoment));

            // If receipt is in the future, don't check time deviation
            yield break;
        }

        // Skip time deviation validation for handwritten receipts which may be backdated
        if (isHandwritten)
        {
            yield break;
        }

        var timeDifference = Math.Abs((receiptMomentUtc - serverTime).TotalMinutes);

        const double maxAllowedDifferenceMinutes = 10.0;

        if (timeDifference > maxAllowedDifferenceMinutes)
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_CbReceiptMomentDeviationExceeded(request.cbReceiptMoment, serverTime, timeDifference),
                "EEEE_CbReceiptMomentDeviationExceeded",
                "cbReceiptMoment"
            )
            .WithContext("ServerTime", serverTime)
            .WithContext("CbReceiptMoment", request.cbReceiptMoment)
            .WithContext("DifferenceInMinutes", timeDifference)
            .WithContext("MaxAllowedDifferenceMinutes", maxAllowedDifferenceMinutes));
        }
    }
}
