using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;

public static class ChargeItemValidations
{
    /// <summary>
    /// Validates that charge items have all mandatory fields set.
    /// Returns one ValidationResult per validation error found.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_MandatoryFields(ReceiptRequest request)
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
                    ErrorMessagesPT.EEEE_ChargeItemValidationFailed(i, "description is missing"),
                    "EEEE_ChargeItemDescriptionMissing",
                    "cbChargeItems.Description",
                    i
                ));
            }

            // VAT Rate validation
            if (chargeItem.VATRate < 0)
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_ChargeItemValidationFailed(i, "VAT rate is missing or invalid"),
                    "EEEE_ChargeItemVATRateMissing",
                    "cbChargeItems.VATRate",
                    i
                ).WithContext("VATRate", chargeItem.VATRate));
            }

            // Amount (price) validation
            if (chargeItem.Amount == 0)
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_ChargeItemValidationFailed(i, "amount (price) is missing or zero"),
                    "EEEE_ChargeItemAmountMissing",
                    "cbChargeItems.Amount",
                    i
                ).WithContext("Amount", chargeItem.Amount));
            }
        }
    }

    /// <summary>
    /// Validates that charge item descriptions are at least 3 characters long.
    /// Returns one ValidationResult per validation error found.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_Description_Length(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];

            if (!string.IsNullOrWhiteSpace(chargeItem.Description) && chargeItem.Description.Length < 3)
            {
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_ChargeItemValidationFailed(i, "description must be at least 3 characters long"),
                    "EEEE_ChargeItemDescriptionTooShort",
                    "cbChargeItems.Description",
                    i
                )
                .WithContext("DescriptionLength", chargeItem.Description.Length)
                .WithContext("MinimumLength", 3));
            }
        }
    }

    /// <summary>
    /// Validates that POS receipt net amount does not exceed 1000€.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_NetAmountLimit(ReceiptRequest request)
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
    /// Validates that only supported VAT rates are used in charge items.
    /// Returns one ValidationResult per unsupported VAT rate found.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_VATRate_SupportedVatRates(ReceiptRequest request)
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

        for (var i = 0; i < request.cbChargeItems.Count; i++)
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
    public static IEnumerable<ValidationResult> Validate_ChargeItems_ftChargeItemCase_SupportedChargeItemCases(ReceiptRequest request)
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

        for (var i = 0; i < request.cbChargeItems.Count; i++)
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
    public static IEnumerable<ValidationResult> Validate_ChargeItems_VATRate_VatRateAndAmount(ReceiptRequest request)
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

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];
            var vatRateCategory = chargeItem.ftChargeItemCase.Vat();

            if (!expectedVatRates.ContainsKey(vatRateCategory))
            {
                continue;
            }

            var expectedVatRatePercentage = expectedVatRates[vatRateCategory];

            if (Math.Abs(chargeItem.VATRate - expectedVatRatePercentage) > 0.001m)
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
                var difference = Math.Abs(chargeItem.VATAmount.Value - calculatedVatAmount);

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
    public static IEnumerable<ValidationResult> Validate_ChargeItems_Amount_Quantity_NegativeAmountsAndQuantities(ReceiptRequest request, bool isRefund)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0 || isRefund)
        {
            yield break;
        }

        for (var i = 0; i < request.cbChargeItems.Count; i++)
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
    /// Validates that charge items with VAT rate 0 have a valid nature of VAT (exempt reason) specified.
    /// Uses the TaxExemptionDictionary to provide detailed validation with proper exemption code references.
    /// Returns one ValidationResult per validation error found.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_VATRate_ZeroVatRateNature(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        // Map nature codes to their corresponding tax exemption codes
        var natureToExemptionMap = new Dictionary<ChargeItemCaseNatureOfVatPT, Constants.TaxExemptionCode>
        {
            { ChargeItemCaseNatureOfVatPT.Group0x30, Constants.TaxExemptionCode.M06 },
            { ChargeItemCaseNatureOfVatPT.Group0x40, Constants.TaxExemptionCode.M16 }
        };

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];

            // Check if VAT rate is 0
            if (Math.Abs(chargeItem.VATRate) < 0.001m)
            {
                // Check if a valid nature of VAT is specified
                var natureOfVat = chargeItem.ftChargeItemCase.NatureOfVat();

                // UsualVatApplies (0x0000) is not valid for zero VAT rate
                if (natureOfVat == ChargeItemCaseNatureOfVatPT.UsualVatApplies)
                {
                    yield return ValidationResult.Failed(new ValidationError(
                        ErrorMessagesPT.EEEE_ZeroVatRateMissingNature(i),
                        "EEEE_ZeroVatRateMissingNature",
                        "cbChargeItems.ftChargeItemCase",
                        i
                    )
                    .WithContext("VATRate", chargeItem.VATRate)
                    .WithContext("NatureOfVat", natureOfVat.ToString())
                    .WithContext("NatureOfVatValue", $"0x{(int) natureOfVat:X4}"));
                }
                else if (natureToExemptionMap.TryGetValue(natureOfVat, out var exemptionCode))
                {
                    // Valid nature is specified, optionally add exemption info to context for logging
                    // This is just for informational purposes and doesn't fail validation
                    if (Constants.TaxExemptionDictionary.TaxExemptionTable.TryGetValue(exemptionCode, out var exemptionInfo))
                    {
                        // Validation passes - exemption is properly specified
                        // You could log this information if needed
                    }
                    else
                    {
                        yield return ValidationResult.Failed(new ValidationError(
                            ErrorMessagesPT.EEEE_UnknownTaxExemptionCode(i, exemptionCode),
                            "EEEE_UnknownTaxExemptionCode",
                            "cbChargeItems.ftChargeItemCase",
                            i
                        )
                        .WithContext("VATRate", chargeItem.VATRate)
                        .WithContext("NatureOfVat", natureOfVat.ToString())
                        .WithContext("NatureOfVatValue", $"0x{(int) natureOfVat:X4}")
                        .WithContext("ExemptionCode", exemptionCode.ToString()));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates that discounts and extras do not exceed the amount of their associated article.
    /// Groups charge items similar to SAFT export: main items with their modifiers (discounts/extras).
    /// Returns one ValidationResult per validation error found.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_DiscountExceedsArticleAmount(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        // Group charge items using the same logic as SAFT export
        var groupedItems = request.GetGroupedChargeItems();

        for (var groupIndex = 0; groupIndex < groupedItems.Count; groupIndex++)
        {
            var group = groupedItems[groupIndex];
            var mainItem = group.chargeItem;
            var modifiers = group.modifiers;

            // Skip validation if there are no modifiers
            if (modifiers == null || modifiers.Count == 0)
            {
                continue;
            }

            // Calculate the main item's net amount
            var mainItemGrossAmount = mainItem.Amount;

            // Calculate total modifiers net amount (discounts and extras)
            var modifiersGrossAmount = modifiers.Sum(x => x.Amount);
            // For discounts (negative amounts), the absolute value should not exceed the main item amount
            if (modifiersGrossAmount < 0)
            {
                var absoluteDiscountAmount = Math.Abs(modifiersGrossAmount);
                var absoluteMainItemNetAmount = Math.Abs(mainItemGrossAmount);

                if (absoluteDiscountAmount > absoluteMainItemNetAmount)
                {
                    // Find the index of the main item for better error reporting
                    var mainItemIndex = request.cbChargeItems.IndexOf(mainItem);
                    
                    yield return ValidationResult.Failed(new ValidationError(
                        ErrorMessagesPT.EEEE_DiscountExceedsArticleAmount(
                            mainItemIndex,
                            mainItem.Description,
                            absoluteDiscountAmount,
                            absoluteMainItemNetAmount
                        ),
                        "EEEE_DiscountExceedsArticleAmount",
                        "cbChargeItems",
                        mainItemIndex
                    )
                    .WithContext("MainItemNetAmount", absoluteMainItemNetAmount)
                    .WithContext("DiscountNetAmount", absoluteDiscountAmount)
                    .WithContext("Difference", absoluteDiscountAmount - absoluteMainItemNetAmount)
                    .WithContext("MainItemDescription", mainItem.Description)
                    .WithContext("ModifierCount", modifiers.Count));
                }
            }
        }
    }
}
