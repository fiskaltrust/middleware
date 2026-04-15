using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Validation;
using FluentValidation;
using FluentValidation.Results;
using ReceiptCaseFlags = fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlags;

namespace fiskaltrust.Middleware.Localization.QueuePT.ValidationFV.Rules;

public class ChargeItemValidations : AbstractValidator<ReceiptRequest>
{
    public ChargeItemValidations()
    {
        Include(new DescriptionMustNotBeEmpty());
        Include(new DescriptionMinLength());
        Include(new VatRateMustNotBeNegative());
        Include(new AmountMustNotBeZero());
        Include(new QuantityMustNotBeZero());
        Include(new PosReceiptNetAmountLimit());
        Include(new OtherServiceNetAmountLimit());
        Include(new SupportedVatRates());
        Include(new SupportedChargeItemCases());
        Include(new VatRateCategory());
        Include(new VatAmountCheck());
        Include(new ZeroVatExemption());
        Include(new DiscountOrExtraNotPositive());
        Include(new DiscountVatRateAndCaseAlignment());
        Include(new ServiceType());
    }

    public class ServiceType : AbstractValidator<ReceiptRequest>
    {
        private static readonly ChargeItemCaseTypeOfService[] SupportedServiceTypes =
        [
            ChargeItemCaseTypeOfService.UnknownService,
            ChargeItemCaseTypeOfService.Delivery,
            ChargeItemCaseTypeOfService.OtherService,
            ChargeItemCaseTypeOfService.Tip,
            ChargeItemCaseTypeOfService.CatalogService,
            ChargeItemCaseTypeOfService.Receivable
        ];

        public ServiceType()
        {
            When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten), () =>
            {
                RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
                {
                    chargeItem.RuleFor(x => x.ftChargeItemCase)
                        .Must(caseValue => SupportedServiceTypes.Contains(caseValue.TypeOfService()))
                        .WithMessage(item => $"Unsupported charge item service type: {item.ftChargeItemCase.TypeOfService()}")
                        .WithErrorCode("UnsupportedChargeItemServiceType");
                });
            });
        }
    }

    public class DescriptionMinLength : AbstractValidator<ReceiptRequest>
    {
        public DescriptionMinLength()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.Description)
                    .MinimumLength(3)
                    .When(x => !string.IsNullOrEmpty(x.Description))
                    .WithErrorCode("ChargeItemDescriptionTooShort")
                    .WithState(_ => new ValidationHelp("Description must be at least 3 characters long."));
            }).When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbChargeItems != null);
        }
    }

    public class PosReceiptNetAmountLimit : AbstractValidator<ReceiptRequest>
    {
        private const decimal Limit = 100m;

        public PosReceiptNetAmountLimit()
        {
            When(x => x.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001)
                    && x.cbChargeItems != null && x.cbChargeItems.Count > 0
                    && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund)
                    && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten), () =>
            {
                RuleFor(x => x.cbChargeItems)
                    .Must(chargeItems =>
                    {
                        var totalNetAmount = chargeItems!.Sum(item => item.Amount - item.GetVATAmount());
                        return totalNetAmount <= Limit;
                    })
                    .WithMessage(request =>
                    {
                        var totalNetAmount = request.cbChargeItems!.Sum(item => item.Amount - item.GetVATAmount());
                        return $"POS receipt net amount ({totalNetAmount:F2}) exceeds limit of {Limit:F2}€";
                    })
                    .WithErrorCode("PosReceiptNetAmountExceedsLimit");
            });
        }
    }

    public class OtherServiceNetAmountLimit : AbstractValidator<ReceiptRequest>
    {
        private const decimal Limit = 100m;

        public OtherServiceNetAmountLimit()
        {
            When(x => x.cbChargeItems != null && x.cbChargeItems.Count > 0
                    && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund)
                    && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten), () =>
            {
                RuleFor(x => x.cbChargeItems)
                    .Must(chargeItems =>
                    {
                        var otherServiceNetAmount = chargeItems!
                            .Where(item => item.ftChargeItemCase.TypeOfService() == ChargeItemCaseTypeOfService.OtherService)
                            .Sum(item => item.Amount - item.GetVATAmount());
                        return otherServiceNetAmount <= Limit;
                    })
                    .WithMessage(request =>
                    {
                        var otherServiceNetAmount = request.cbChargeItems!
                            .Where(item => item.ftChargeItemCase.TypeOfService() == ChargeItemCaseTypeOfService.OtherService)
                            .Sum(item => item.Amount - item.GetVATAmount());
                        return $"OtherService net amount ({otherServiceNetAmount:F2}) exceeds limit of {Limit:F2}€";
                    })
                    .WithErrorCode("OtherServiceNetAmountExceedsLimit");
            });
        }
    }

    public class SupportedVatRates : AbstractValidator<ReceiptRequest>
    {
        private static readonly ChargeItemCase[] _unsupported =
        [
            ChargeItemCase.UnknownService,
            ChargeItemCase.SuperReducedVatRate1,
            ChargeItemCase.SuperReducedVatRate2,
            ChargeItemCase.ParkingVatRate,
            ChargeItemCase.ZeroVatRate
        ];

        public SupportedVatRates()
        {
            When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbChargeItems != null, () =>
            {
                RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
                {
                    chargeItem.RuleFor(x => x.ftChargeItemCase)
                        .Must(ftCase => !_unsupported.Contains(ftCase.Vat()))
                        .WithMessage("Unsupported VAT rate category. Portugal supports: DiscountedVatRate1 (6%), DiscountedVatRate2 (13%), NormalVatRate (23%), and NotTaxable (0%).")
                        .WithErrorCode("UnsupportedVatRate");
                });
            });
        }
    }

    public class VatRateCategory : AbstractValidator<ReceiptRequest>
    {
        private static readonly Dictionary<ChargeItemCase, decimal> _expectedRates = new()
        {
            { ChargeItemCase.DiscountedVatRate1, 6.0m },
            { ChargeItemCase.DiscountedVatRate2, 13.0m },
            { ChargeItemCase.NormalVatRate, 23.0m },
            { ChargeItemCase.NotTaxable, 0.0m }
        };

        public VatRateCategory()
        {
            When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbChargeItems != null, () =>
            {
                RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
                {
                    chargeItem.RuleFor(x => x.VATRate)
                        .Must((item, vatRate) =>
                        {
                            var vatCategory = item.ftChargeItemCase.Vat();
                            if (!_expectedRates.TryGetValue(vatCategory, out var expectedRate))
                                return true;
                            return Math.Abs(vatRate - expectedRate) <= 0.001m;
                        })
                        .WithMessage(item =>
                        {
                            var vatCategory = item.ftChargeItemCase.Vat();
                            _expectedRates.TryGetValue(vatCategory, out var expectedRate);
                            return $"VAT rate category '{vatCategory}' expects {expectedRate}% but VATRate is {item.VATRate}%.";
                        })
                        .WithErrorCode("VatRateMismatch");
                });
            });
        }
    }

    public class VatAmountCheck : AbstractValidator<ReceiptRequest>
    {
        private const decimal RoundingTolerance = 0.01m;

        public VatAmountCheck()
        {
            When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbChargeItems != null, () =>
            {
                RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
                {
                    chargeItem.RuleFor(x => x.VATAmount)
                        .Must((item, vatAmount) =>
                        {
                            if (!vatAmount.HasValue || item.VATRate <= 0)
                                return true;
                            var calculated = item.Amount / (100 + item.VATRate) * item.VATRate;
                            return Math.Abs(vatAmount.Value - calculated) <= RoundingTolerance;
                        })
                        .WithMessage(item =>
                        {
                            var calculated = item.Amount / (100 + item.VATRate) * item.VATRate;
                            return $"VATAmount {item.VATAmount:F4} does not match calculated {calculated:F4} (difference exceeds {RoundingTolerance}€).";
                        })
                        .WithErrorCode("VatAmountMismatch");
                });
            });
        }
    }

    public class ZeroVatExemption : AbstractValidator<ReceiptRequest>
    {
        public ZeroVatExemption()
        {
            When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbChargeItems != null, () =>
            {
                RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
                {
                    chargeItem.RuleFor(x => x.VATRate)
                        .Must((item, vatRate) =>
                        {
                            if (Math.Abs(vatRate) > 0.001m)
                                return true;

                            var natureValue = item.ftChargeItemCase.NatureOfVat();
                            if (natureValue == ChargeItemCaseNatureOfVatPT.UsualVatApplies)
                                return false;

                            var exemptionCode = (TaxExemptionCodePT) (int) natureValue;
                            return TaxExemptionDictionaryPT.TaxExemptionTable.ContainsKey(exemptionCode);
                        })
                        .WithMessage(item =>
                        {
                            var natureValue = item.ftChargeItemCase.NatureOfVat();
                            if (natureValue == ChargeItemCaseNatureOfVatPT.UsualVatApplies)
                                return "Zero VAT rate requires a valid tax exemption reason via the Nature of VAT field.";
                            return $"Unknown tax exemption code '0x{(int) natureValue:X4}'.";
                        })
                        .WithErrorCode("ZeroVatExemption");
                });
            });
        }
    }

    public class DescriptionMustNotBeEmpty : AbstractValidator<ReceiptRequest>
    {
        public DescriptionMustNotBeEmpty()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.Description)
                    .Must(desc => !string.IsNullOrWhiteSpace(desc))
                    .WithMessage("Description is missing.")
                    .WithErrorCode("ChargeItemDescriptionMissing")
                    .WithState(_ => new ValidationHelp("Provide a non-empty Description for each charge item."));
            }).When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbChargeItems != null);
        }
    }

    public class VatRateMustNotBeNegative : AbstractValidator<ReceiptRequest>
    {
        public VatRateMustNotBeNegative()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.VATRate)
                    .Must(rate => rate >= 0)
                    .WithMessage(item => $"VATRate {item.VATRate} is invalid (must be >= 0).")
                    .WithErrorCode("ChargeItemVatRateMissing")
                    .WithState(_ => new ValidationHelp("Set VATRate to 0 or a positive percentage. Supported rates for Portugal: 6, 13, 23, or 0."));
            }).When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbChargeItems != null);
        }
    }

    public class AmountMustNotBeZero : AbstractValidator<ReceiptRequest>
    {
        public AmountMustNotBeZero()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.Amount)
                    .Must(amount => amount != 0)
                    .WithMessage(item => $"Amount {item.Amount} is zero.")
                    .WithErrorCode("ChargeItemAmountMissing")
                    .WithState(_ => new ValidationHelp("Use a positive Amount for sales and a negative Amount for refund items."));
            }).When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbChargeItems != null);
        }
    }

    public class QuantityMustNotBeZero : AbstractValidator<ReceiptRequest>
    {
        public QuantityMustNotBeZero()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.Quantity)
                    .Must(qty => qty != 0)
                    .WithMessage(item => $"Quantity {item.Quantity} is zero.")
                    .WithErrorCode("ChargeItemQuantityZeroNotAllowed")
                    .WithState(_ => new ValidationHelp("Use a positive Quantity for sales and a negative Quantity for returns."));
            }).When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbChargeItems != null);
        }
    }

    public class SupportedChargeItemCases : AbstractValidator<ReceiptRequest>
    {
        private static readonly ChargeItemCaseTypeOfService[] _supported =
        [
            ChargeItemCaseTypeOfService.UnknownService,
            ChargeItemCaseTypeOfService.Delivery,
            ChargeItemCaseTypeOfService.OtherService,
            ChargeItemCaseTypeOfService.Tip,
            ChargeItemCaseTypeOfService.CatalogService,
            ChargeItemCaseTypeOfService.Receivable
        ];

        public SupportedChargeItemCases()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.ftChargeItemCase)
                    .Must(ftCase => _supported.Contains(ftCase.TypeOfService()))
                    .WithMessage(item => $"Unsupported ChargeItemCase TypeOfService '{item.ftChargeItemCase.TypeOfService()}'.")
                    .WithErrorCode("UnsupportedChargeItemServiceType");
            }).When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten) && x.cbChargeItems != null);
        }
    }

    public class DiscountOrExtraNotPositive : AbstractValidator<ReceiptRequest>
    {
        public DiscountOrExtraNotPositive()
        {
            When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                    && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund)
                    && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void)
                    && x.cbChargeItems != null && x.cbChargeItems.Count > 0
                    && !x.IsPartialRefundReceipt(), () =>
            {
                RuleFor(x => x)
                    .Custom((request, context) =>
                    {
                        for (var i = 0; i < request.cbChargeItems!.Count; i++)
                        {
                            var item = request.cbChargeItems[i];
                            if (item.IsDiscountOrExtra() && item.Amount > 0)
                                context.AddFailure(new ValidationFailure(
                                    $"cbChargeItems[{i}].Amount",
                                    $"Discount/extra amount {item.Amount} must not be positive.")
                                { ErrorCode = "PositiveDiscountNotAllowed" });
                        }
                    });
            });
        }
    }

    public class DiscountVatRateAndCaseAlignment : AbstractValidator<ReceiptRequest>
    {
        public DiscountVatRateAndCaseAlignment()
        {
            When(x => !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten)
                    && x.cbChargeItems != null && x.cbChargeItems.Count > 0, () =>
            {
                RuleFor(x => x)
                    .Custom((request, context) =>
                    {
                        var groupedItems = request.GetGroupedChargeItems();
                        foreach (var group in groupedItems)
                        {
                            var mainItem = group.chargeItem;
                            var modifiers = group.modifiers;
                            if (modifiers == null || modifiers.Count == 0)
                                continue;
                            var mainVatRate = mainItem.VATRate;
                            var mainVatCase = mainItem.ftChargeItemCase.Vat();
                            var mainItemIndex = request.cbChargeItems!.IndexOf(mainItem);
                            foreach (var modifier in modifiers.Where(m => m.IsDiscountOrExtra()))
                            {
                                var modifierVatCase = modifier.ftChargeItemCase.Vat();
                                var modifierIndex = request.cbChargeItems.IndexOf(modifier);
                                var vatRateMismatch = Math.Abs(modifier.VATRate - mainVatRate) > 0.001m;
                                var vatCaseMismatch = modifierVatCase != mainVatCase;
                                if (!vatRateMismatch && !vatCaseMismatch)
                                    continue;
                                context.AddFailure(new ValidationFailure(
                                    $"cbChargeItems[{modifierIndex}]",
                                    $"Discount/extra at index {modifierIndex} has VAT rate {modifier.VATRate} / case '{modifierVatCase}' " +
                                    $"but main item at index {mainItemIndex} has VAT rate {mainVatRate} / case '{mainVatCase}'.")
                                { ErrorCode = "DiscountVatRateOrCaseMismatch" });
                            }
                        }
                    });
            });
        }
    }

}
