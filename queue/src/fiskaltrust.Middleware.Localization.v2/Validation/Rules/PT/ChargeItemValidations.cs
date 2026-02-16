using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Models.Cases;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;

public class ChargeItemValidations : AbstractValidator<ReceiptRequest>
{
    public ChargeItemValidations()
    {
        Include(new DescriptionMinLength());
        Include(new PosReceiptNetAmountLimit());
        Include(new OtherServiceNetAmountLimit());
        Include(new SupportedVatRates());
        Include(new VatRateCategory());
        Include(new ZeroVatExemption());
    }

    public class DescriptionMinLength : AbstractValidator<ReceiptRequest>
    {
        public DescriptionMinLength()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.Description)
                    .MinimumLength(3)
                    .When(x => !string.IsNullOrEmpty(x.Description));
            });
        }
    }

    public class PosReceiptNetAmountLimit : AbstractValidator<ReceiptRequest>
    {
        private const decimal Limit = 1000m;

        public PosReceiptNetAmountLimit()
        {
            When(x => x.ftReceiptCase.IsCase(ReceiptCase.PointOfSaleReceipt0x0001)
                    && x.cbChargeItems != null && x.cbChargeItems.Count > 0, () =>
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
            When(x => x.cbChargeItems != null && x.cbChargeItems.Count > 0, () =>
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
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.ftChargeItemCase)
                    .Must(ftCase => !_unsupported.Contains(ftCase.Vat()))
                    .WithMessage("Unsupported VAT rate category. Portugal supports: DiscountedVatRate1 (6%), DiscountedVatRate2 (13%), NormalVatRate (23%), and NotTaxable (0%).")
                    .WithErrorCode("UnsupportedVatRate");
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
        }
    }

    public class ZeroVatExemption : AbstractValidator<ReceiptRequest>
    {
        public ZeroVatExemption()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.VATRate)
                    .Must((item, vatRate) =>
                    {
                        if (Math.Abs(vatRate) > 0.001m)
                            return true;

                        var natureValue = item.ftChargeItemCase.NatureOfVatPT();
                        if (natureValue == ChargeItemCaseNatureOfVatPT.UsualVatApplies)
                            return false;

                        var exemptionCode = (TaxExemptionCodePT)(int)natureValue;
                        return TaxExemptionDictionaryPT.TaxExemptionTable.ContainsKey(exemptionCode);
                    })
                    .WithMessage(item =>
                    {
                        var natureValue = item.ftChargeItemCase.NatureOfVatPT();
                        if (natureValue == ChargeItemCaseNatureOfVatPT.UsualVatApplies)
                            return "Zero VAT rate requires a valid tax exemption reason via the Nature of VAT field.";
                        return $"Unknown tax exemption code '0x{(int)natureValue:X4}'.";
                    })
                    .WithErrorCode("ZeroVatExemption");
            });
        }
    }
}
