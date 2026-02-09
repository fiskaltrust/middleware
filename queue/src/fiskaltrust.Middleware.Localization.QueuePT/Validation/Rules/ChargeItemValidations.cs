using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Models;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using System.Text;

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
                var rule = PortugalValidationRules.ChargeItemDescriptionMissing;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_ChargeItemValidationFailed(i, "description is missing"),
                    rule.Code,
                    rule.Field,
                    i
                ));
            }

            // VAT Rate validation
            if (chargeItem.VATRate < 0)
            {
                var rule = PortugalValidationRules.ChargeItemVatRateMissing;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_ChargeItemValidationFailed(i, "VAT rate is missing or invalid"),
                    rule.Code,
                    rule.Field,
                    i
                ).WithContext("VATRate", chargeItem.VATRate));
            }

            // Amount (price) validation
            if (chargeItem.Amount == 0)
            {
                var rule = PortugalValidationRules.ChargeItemAmountMissing;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_ChargeItemValidationFailed(i, "amount (price) is missing or zero"),
                    rule.Code,
                    rule.Field,
                    i
                ).WithContext("Amount", chargeItem.Amount));
            }
        }

    }

    /// <summary>
    /// Validates that charge item descriptions can be encoded using Windows-1252.
    /// Returns one ValidationResult per validation error found.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_Description_Encoding(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        var encoding = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];

            if (string.IsNullOrEmpty(chargeItem.Description))
            {
                continue;
            }

            var hasEncodingIssue = false;
            try
            {
                _ = encoding.GetBytes(chargeItem.Description);
            }
            catch (EncoderFallbackException)
            {
                hasEncodingIssue = true;
            }

            if (hasEncodingIssue)
            {
                var rule = PortugalValidationRules.ChargeItemDescriptionEncodingInvalid;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_ChargeItemDescriptionEncodingInvalid(i),
                    rule.Code,
                    rule.Field,
                    i
                ).WithContext("Description", chargeItem.Description));
            }
        }
    }

    /// <summary>
    /// Validates that charge items do not have zero quantity.
    /// Returns one ValidationResult per validation error found.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_Quantity_NotZero(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];

            if (chargeItem.Quantity == 0)
            {
                var rule = PortugalValidationRules.ChargeItemQuantityZeroNotAllowed;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_ChargeItemQuantityZeroNotAllowed,
                    rule.Code,
                    rule.Field,
                    i
                ).WithContext("Quantity", chargeItem.Quantity));
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
                var rule = PortugalValidationRules.ChargeItemDescriptionTooShort;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_ChargeItemValidationFailed(i, "description must be at least 3 characters long"),
                    rule.Code,
                    rule.Field,
                    i
                )
                .WithContext("DescriptionLength", chargeItem.Description.Length)
                .WithContext("MinimumLength", 3));
            }
        }
    }

    /// <summary>
    /// Validates that POS receipt net amount does not exceed 100€.
    /// Returns a single ValidationResult if validation fails.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_NetAmountLimit(ReceiptRequest request)
    {
        if (!request.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001))
        {
            yield break;
        }

        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        var totalNetAmount = request.cbChargeItems
            .Sum(chargeItem => chargeItem.Amount - chargeItem.GetVATAmount());

        if (totalNetAmount > 100m)
        {
            var rule = PortugalValidationRules.PosReceiptNetAmountExceedsLimit;
            yield return ValidationResult.Failed(new ValidationError(
                ErrorMessagesPT.EEEE_PosReceiptNetAmountExceedsLimit,
                rule.Code,
                rule.Field
            ).WithContext("TotalNetAmount", totalNetAmount)
             .WithContext("Limit", 100m));
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
                var rule = PortugalValidationRules.UnsupportedVatRate;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_UnsupportedVatRate(i, vatRate),
                    rule.Code,
                    rule.Field,
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
            ChargeItemCaseTypeOfService.CatalogService,
            ChargeItemCaseTypeOfService.Receivable
        };

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];
            var serviceType = chargeItem.ftChargeItemCase.TypeOfService();

            if (!supportedServiceTypes.Contains(serviceType))
            {
                var rule = PortugalValidationRules.UnsupportedChargeItemServiceType;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_UnsupportedChargeItemServiceType(i, serviceType),
                    rule.Code,
                    rule.Field,
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
                var rule = PortugalValidationRules.VatRateMismatch;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_VatRateMismatch(i, vatRateCategory, expectedVatRatePercentage, chargeItem.VATRate),
                    rule.Code,
                    rule.Field,
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
                    var rule = PortugalValidationRules.VatAmountMismatch;
                    yield return ValidationResult.Failed(new ValidationError(
                        ErrorMessagesPT.EEEE_VatAmountMismatch(i, chargeItem.VATAmount.Value, calculatedVatAmount, difference),
                        rule.Code,
                        rule.Field,
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
    /// Validates that discounts/extras are not positive (Portugal does not allow positive discounts).
    /// Returns one ValidationResult per offending charge item.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_DiscountOrExtra_NotPositive(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];

            if (chargeItem.IsDiscountOrExtra() && chargeItem.Amount > 0)
            {
                var rule = PortugalValidationRules.PositiveDiscountNotAllowed;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_PositiveDiscountNotAllowed(i, chargeItem.Amount),
                    rule.Code,
                    rule.Field,
                    i
                ).WithContext("Amount", chargeItem.Amount));
            }
        }
    }

    /// <summary>
    /// Validates that non-refund receipts do not have negative quantities or amounts.
    /// Returns one ValidationResult per validation error found.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_Amount_Quantity_NegativeAmountsAndQuantities(ReceiptRequest request, bool isRefund)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0 || isRefund || request.IsPartialRefundReceipt() || request.ftReceiptCase.IsFlag(ifPOS.v2.Cases.ReceiptCaseFlags.Void))
        {
            yield break;
        }

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];

            if (chargeItem.IsDiscount() || chargeItem.IsRefund() || chargeItem.IsVoid())
            {
                continue;
            }

            if (chargeItem.Quantity < 0)
            {
                var rule = PortugalValidationRules.NegativeQuantityNotAllowed;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_NegativeQuantityNotAllowed(i, chargeItem.Quantity),
                    rule.Code,
                    rule.Field,
                    i
                ).WithContext("Quantity", chargeItem.Quantity));
            }

            if (chargeItem.Amount < 0)
            {
                var rule = PortugalValidationRules.NegativeAmountNotAllowed;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_NegativeAmountNotAllowed(i, chargeItem.Amount),
                    rule.Code,
                    rule.Field,
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

        for (var i = 0; i < request.cbChargeItems.Count; i++)
        {
            var chargeItem = request.cbChargeItems[i];
            if (chargeItem.ftChargeItemCase.TypeOfService() == ChargeItemCaseTypeOfService.Receivable)
            {
                continue;
            }

            // Check if VAT rate is 0
            if (Math.Abs(chargeItem.VATRate) < 0.001m)
            {
                // Check if a valid nature of VAT is specified
                var natureOfVat = chargeItem.ftChargeItemCase.NatureOfVat();

                // UsualVatApplies (0x0000) is not valid for zero VAT rate
                if (natureOfVat == ChargeItemCaseNatureOfVatPT.UsualVatApplies)
                {
                    var rule = PortugalValidationRules.ZeroVatRateMissingNature;
                    yield return ValidationResult.Failed(new ValidationError(
                        ErrorMessagesPT.EEEE_ZeroVatRateMissingNature(i),
                        rule.Code,
                        rule.Field,
                        i
                    )
                    .WithContext("VATRate", chargeItem.VATRate)
                    .WithContext("NatureOfVat", natureOfVat.ToString())
                    .WithContext("NatureOfVatValue", $"0x{(int) natureOfVat:X4}"));
                }
                else
                {
                    var exemptionCode = (int) chargeItem.ftChargeItemCase.NatureOfVat();
                    if (Constants.TaxExemptionDictionary.TaxExemptionTable.ContainsKey((Constants.TaxExemptionCode) exemptionCode))
                    {
                    }
                    else
                    {
                        var rule = PortugalValidationRules.UnknownTaxExemptionCode;
                        yield return ValidationResult.Failed(new ValidationError(
                              ErrorMessagesPT.EEEE_UnknownTaxExemptionCode(i, (Constants.TaxExemptionCode) exemptionCode),
                              rule.Code,
                              rule.Field,
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
    /// Validates that discounts/extras use the same VAT rate and VAT case as their related line item.
    /// Returns one ValidationResult per mismatch found.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate_ChargeItems_DiscountVatRateAndCaseAlignment(ReceiptRequest request)
    {
        if (request.cbChargeItems == null || request.cbChargeItems.Count == 0)
        {
            yield break;
        }

        var groupedItems = request.GetGroupedChargeItems();

        for (var groupIndex = 0; groupIndex < groupedItems.Count; groupIndex++)
        {
            var group = groupedItems[groupIndex];
            var mainItem = group.chargeItem;
            var modifiers = group.modifiers;

            if (modifiers == null || modifiers.Count == 0)
            {
                continue;
            }

            var mainVatRate = mainItem.VATRate;
            var mainVatCase = mainItem.ftChargeItemCase.Vat();
            var mainItemIndex = request.cbChargeItems.IndexOf(mainItem);

            foreach (var modifier in modifiers.Where(x => x.IsDiscountOrExtra()))
            {
                var modifierVatCase = modifier.ftChargeItemCase.Vat();
                var modifierIndex = request.cbChargeItems.IndexOf(modifier);

                var vatRateMismatch = Math.Abs(modifier.VATRate - mainVatRate) > 0.001m;
                var vatCaseMismatch = modifierVatCase != mainVatCase;

                if (!vatRateMismatch && !vatCaseMismatch)
                {
                    continue;
                }

                var rule = PortugalValidationRules.DiscountVatRateOrCaseMismatch;
                yield return ValidationResult.Failed(new ValidationError(
                    ErrorMessagesPT.EEEE_DiscountVatRateOrCaseMismatch(mainItemIndex, modifierIndex, mainVatRate, modifier.VATRate, mainVatCase, modifierVatCase),
                    rule.Code,
                    rule.Field,
                    modifierIndex
                )
                .WithContext("MainItemIndex", mainItemIndex)
                .WithContext("ModifierIndex", modifierIndex)
                .WithContext("MainVatRate", mainVatRate)
                .WithContext("ModifierVatRate", modifier.VATRate)
                .WithContext("MainVatCase", mainVatCase.ToString())
                .WithContext("ModifierVatCase", modifierVatCase.ToString()));
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
                    var rule = PortugalValidationRules.DiscountExceedsArticleAmount;
                    // Find the index of the main item for better error reporting
                    var mainItemIndex = request.cbChargeItems.IndexOf(mainItem);

                    yield return ValidationResult.Failed(new ValidationError(
                        ErrorMessagesPT.EEEE_DiscountExceedsArticleAmount(
                            mainItemIndex,
                            mainItem.Description,
                            absoluteDiscountAmount,
                            absoluteMainItemNetAmount
                        ),
                        rule.Code,
                        rule.Field,
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
