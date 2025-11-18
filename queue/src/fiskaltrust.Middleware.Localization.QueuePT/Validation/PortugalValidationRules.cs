using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation;

/// <summary>
/// Shared validation logic for Portugal receipts.
/// These methods return IEnumerable of ValidationResult objects, with one result per error.
/// </summary>
public static class PortugalValidationRules
{
    /// <summary>
    /// Validates that only supported VAT rates are used in charge items.
    /// Returns one ValidationResult per unsupported VAT rate found.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateSupportedVatRates(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
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
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_UnsupportedVatRate(i, vatRate),
                    "EEEE_UnsupportedVatRate",
                    "cbChargeItems",
                    i
                ).WithContext("VatRate", vatRate.ToString()));
            }
        }
    }

    /// <summary>
    /// Validates that only supported charge item cases are used.
    /// Returns one ValidationResult per unsupported service type found.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateSupportedChargeItemCases(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        var supportedServiceTypes = new[]
        {
            ChargeItemCaseTypeOfService.UnknownService,
            ChargeItemCaseTypeOfService.Delivery,
            ChargeItemCaseTypeOfService.OtherService,
            ChargeItemCaseTypeOfService.Tip,
            ChargeItemCaseTypeOfService.CatalogService
        };

        for (int i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];
            var serviceType = chargeItem.ftChargeItemCase.TypeOfService();

            if (!supportedServiceTypes.Contains(serviceType))
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_UnsupportedChargeItemServiceType(i, serviceType),
                    "EEEE_UnsupportedChargeItemServiceType",
                    "cbChargeItems",
                    i
                ).WithContext("ServiceType", serviceType.ToString()));
            }
        }
    }

    /// <summary>
    /// Validates that the VAT rate category matches the specified VAT rate percentage,
    /// and that the VAT amount is correctly calculated.
    /// Returns one ValidationResult per validation error found.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateVatRateAndAmount(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        var expectedVatRates = new Dictionary<ChargeItemCase, decimal>
        {
            { ChargeItemCase.DiscountedVatRate1, 6.0m },
            { ChargeItemCase.DiscountedVatRate2, 13.0m },
            { ChargeItemCase.NormalVatRate, 23.0m },
            { ChargeItemCase.NotTaxable, 0.0m }
        };

        const decimal roundingTolerance = 0.01m;

        for (int i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];
            var vatRateCategory = chargeItem.ftChargeItemCase.Vat();

            if (!expectedVatRates.ContainsKey(vatRateCategory))
            {
                continue;
            }

            var expectedVatRatePercentage = expectedVatRates[vatRateCategory];

            if (System.Math.Abs(chargeItem.VATRate - expectedVatRatePercentage) > 0.001m)
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_VatRateMismatch(i, vatRateCategory, expectedVatRatePercentage, chargeItem.VATRate),
                    "EEEE_VatRateMismatch",
                    "cbChargeItems.VATRate",
                    i
                )
                .WithContext("VatRateCategory", vatRateCategory.ToString())
                .WithContext("ExpectedVatRate", expectedVatRatePercentage)
                .WithContext("ActualVatRate", chargeItem.VATRate));
            }

            if (chargeItem.VATAmount.HasValue && chargeItem.VATRate > 0)
            {
                var calculatedVatAmount = chargeItem.Amount / (100 + chargeItem.VATRate) * chargeItem.VATRate;
                var difference = System.Math.Abs(chargeItem.VATAmount.Value - calculatedVatAmount);

                if (difference > roundingTolerance)
                {
                    yield return ValidationResult.Failed(new ValidationError(
                        ErrorMessagesPT.EEEE_VatAmountMismatch(i, chargeItem.VATAmount.Value, calculatedVatAmount, difference),
                        "EEEE_VatAmountMismatch",
                        "cbChargeItems.VATAmount",
                        i
                    )
                    .WithContext("ProvidedVatAmount", chargeItem.VATAmount.Value)
                    .WithContext("CalculatedVatAmount", calculatedVatAmount)
                    .WithContext("Difference", difference));
                }
            }
        }
    }

    /// <summary>
    /// Validates that non-refund receipts do not have negative quantities or amounts.
    /// Returns one ValidationResult per validation error found.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateNegativeAmountsAndQuantities(ReceiptRequest request, bool isRefund)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0 || isRefund)
        {
            yield break;
        }

        for (int i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];

            if (chargeItem.IsDiscount())
            {
                continue;
            }

            if (chargeItem.Quantity < 0)
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_NegativeQuantityNotAllowed(i, chargeItem.Quantity),
                    "EEEE_NegativeQuantityNotAllowed",
                    "cbChargeItems.Quantity",
                    i
                ).WithContext("Quantity", chargeItem.Quantity));
            }

            if (chargeItem.Amount < 0)
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_NegativeAmountNotAllowed(i, chargeItem.Amount),
                    "EEEE_NegativeAmountNotAllowed",
                    "cbChargeItems.Amount",
                    i
                ).WithContext("Amount", chargeItem.Amount));
            }
        }
    }

    /// <summary>
    /// Validates that the sum of charge items matches the sum of pay items.
    /// Returns a single ValidationResult if balance validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateReceiptBalance(ReceiptRequest request)
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

        const decimal roundingTolerance = 0.01m;
        var difference = System.Math.Abs(chargeItemsSum - payItemsSum);

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
    /// Validates that cbUser follows the required PTUserObject structure.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateUserStructure(ReceiptRequest request)
    {
        if (request.cbUser == null)
        {
            yield break;
        }

        // Use a list to collect results since we can't yield from try-catch
        var results = new List<ValidationResult>();

        try
        {
            var userJson = System.Text.Json.JsonSerializer.Serialize(request.cbUser);
            var userObject = System.Text.Json.JsonSerializer.Deserialize<Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401.PTUserObject>(userJson);

            if (userObject == null)
            {
                results.Add(ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_InvalidUserStructure("cbUser could not be deserialized to PTUserObject structure."),
                    "EEEE_InvalidUserStructure",
                    "cbUser"
                )));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(userObject.UserId))
                {
                    results.Add(ValidationResult.Failed(new ValidationError(
                        ErrorMessagesPT.EEEE_InvalidUserStructure("cbUser must contain a non-empty 'UserId' property."),
                        "EEEE_InvalidUserStructure",
                        "cbUser.UserId"
                    )));
                }

                if (!string.IsNullOrWhiteSpace(userObject.UserDisplayName) && userObject.UserDisplayName.Length < 3)
                {
                    results.Add(ValidationResult.Failed(new ValidationError(
                        ErrorMessagesPT.EEEE_InvalidUserStructure($"cbUser.UserDisplayName must be at least 3 characters long. Current length: {userObject.UserDisplayName.Length}"),
                        "EEEE_InvalidUserStructure",
                        "cbUser.UserDisplayName"
                    ).WithContext("ActualLength", userObject.UserDisplayName.Length)
                     .WithContext("MinimumLength", 3)));
                }
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            results.Add(ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_InvalidUserStructure($"cbUser format is invalid: {ex.Message}"),
                "EEEE_InvalidUserStructure",
                "cbUser"
            ).WithContext("ExceptionMessage", ex.Message)));
        }

        foreach (var result in results)
        {
            yield return result;
        }
    }

    /// <summary>
    /// Validates that cbUser is present for receipts that generate signatures.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateUserPresenceForSignatures(ReceiptRequest request, bool generatesSignature)
    {
        if (!generatesSignature)
        {
            yield break;
        }

        if (request.cbUser == null)
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_UserRequiredForSignatures,
                "EEEE_UserRequiredForSignatures",
                "cbUser"
            ));
        }
    }

    /// <summary>
    /// Validates that cash payments do not exceed 3000€.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateCashPaymentLimit(ReceiptRequest request)
    {
        if (request.cbPayItems == null || request.cbPayItems.Count == 0)
        {
            yield break;
        }

        var totalCashAmount = request.cbPayItems
            .Where(payItem => payItem.ftPayItemCase.Case() == PayItemCase.CashPayment)
            .Sum(payItem => payItem.Amount);

        if (totalCashAmount > 3000m)
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_CashPaymentExceedsLimit,
                "EEEE_CashPaymentExceedsLimit",
                "cbPayItems"
            ).WithContext("TotalCashAmount", totalCashAmount)
             .WithContext("Limit", 3000m));
        }
    }

    /// <summary>
    /// Validates that POS receipt net amount does not exceed 1000€.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidatePosReceiptNetAmountLimit(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        var totalNetAmount = request.cbChargeItems
            .Sum(chargeItem => chargeItem.Amount - chargeItem.GetVATAmount());

        if (totalNetAmount > 1000m)
        {
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_PosReceiptNetAmountExceedsLimit,
                "EEEE_PosReceiptNetAmountExceedsLimit",
                "cbChargeItems"
            ).WithContext("TotalNetAmount", totalNetAmount)
             .WithContext("Limit", 1000m));
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
    /// Validates that receipt moment is not more than 10 minutes different from the current server time.
    /// This ensures receipts are created with accurate timestamps (except for handwritten receipts which may be backdated).
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> ValidateReceiptMomentOrder(ReceiptRequest request, object series, bool isHandwritten)
    {
        // Skip validation for handwritten receipts which may be backdated
        if (isHandwritten)
        {
            yield break;
        }

        var serverTime = DateTime.UtcNow;
        var timeDifference = Math.Abs((request.cbReceiptMoment.ToUniversalTime() - serverTime).TotalMinutes);
        
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
