using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Models.Cases;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.ES;

public class ChargeItemValidations : AbstractValidator<ReceiptRequest>
{
    public ChargeItemValidations()
    {
        Include(new VatAmountRequired());
        Include(new SupportedVatRates());
        Include(new VatRateCategory());
        Include(new ZeroVatNature());
    }

    public class VatAmountRequired : AbstractValidator<ReceiptRequest>
    {
        public VatAmountRequired()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.VATAmount).NotNull();
            });
        }
    }

    public class SupportedVatRates : AbstractValidator<ReceiptRequest>
    {
        private static readonly ChargeItemCase[] _unsupported =
        [
            ChargeItemCase.UnknownService,
            ChargeItemCase.ParkingVatRate
        ];

        public SupportedVatRates()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.ftChargeItemCase)
                    .Must(ftCase => !_unsupported.Contains(ftCase.Vat()))
                    .WithMessage("Unsupported VAT rate category. Spain supports: SuperReducedVatRate1/2 (4%), DiscountedVatRate1/2 (10%), NormalVatRate (21%), NotTaxable (0%), and ZeroVatRate (0%).")
                    .WithErrorCode("UnsupportedVatRate");
            });
        }
    }

    public class VatRateCategory : AbstractValidator<ReceiptRequest>
    {
        private static readonly Dictionary<ChargeItemCase, decimal> _expectedRates = new()
        {
            { ChargeItemCase.DiscountedVatRate1, 10.0m },
            { ChargeItemCase.DiscountedVatRate2, 10.0m },
            { ChargeItemCase.SuperReducedVatRate1, 4.0m },
            { ChargeItemCase.SuperReducedVatRate2, 4.0m },
            { ChargeItemCase.NormalVatRate, 21.0m },
            { ChargeItemCase.NotTaxable, 0.0m },
            { ChargeItemCase.ZeroVatRate, 0.0m }
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

    public class ZeroVatNature : AbstractValidator<ReceiptRequest>
    {
        public ZeroVatNature()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.VATRate)
                    .Must((item, vatRate) =>
                    {
                        if (Math.Abs(vatRate) > 0.001m)
                            return true;

                        var natureValue = item.ftChargeItemCase.NatureOfVatES();
                        if (natureValue == ChargeItemCaseNatureOfVatES.UsualVatApplies)
                            return false;

                        return Enum.IsDefined(natureValue);
                    })
                    .WithMessage(item =>
                    {
                        var natureValue = item.ftChargeItemCase.NatureOfVatES();
                        if (natureValue == ChargeItemCaseNatureOfVatES.UsualVatApplies)
                            return "Zero VAT rate requires a valid exemption reason via the Nature of VAT field.";
                        return $"Unknown tax exemption code '0x{(long)natureValue:X4}'.";
                    })
                    .WithErrorCode("ZeroVatNature");
            });
        }
    }
}
