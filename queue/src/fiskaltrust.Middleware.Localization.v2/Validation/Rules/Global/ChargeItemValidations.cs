using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;

public class ChargeItemValidations : AbstractValidator<ReceiptRequest>
{
    public ChargeItemValidations()
    {
        Include(new MandatoryFields());
        Include(new ServiceType());
        Include(new VatCalculation());
        Include(new NegativeAmounts());
        Include(new DiscountLimit());
    }

    public class MandatoryFields : AbstractValidator<ReceiptRequest>
    {
        public MandatoryFields()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.Description).NotEmpty();
                chargeItem.RuleFor(x => x.VATRate).GreaterThanOrEqualTo(0);
                chargeItem.RuleFor(x => x.Amount).NotEqual(0m);
            });
        }
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
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.ftChargeItemCase)
                    .Must(caseValue => SupportedServiceTypes.Contains(caseValue.TypeOfService()))
                    .WithMessage(item => $"Unsupported charge item service type: {item.ftChargeItemCase.TypeOfService()}")
                    .WithErrorCode("UnsupportedChargeItemServiceType");
            });
        }
    }

    public class VatCalculation : AbstractValidator<ReceiptRequest>
    {
        private const decimal RoundingTolerance = 0.01m;

        public VatCalculation()
        {
            RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
            {
                chargeItem.RuleFor(x => x.VATAmount)
                    .Must((item, vatAmount) =>
                    {
                        var calculated = item.Amount / (100 + item.VATRate) * item.VATRate;
                        return Math.Abs(vatAmount!.Value - calculated) <= RoundingTolerance;
                    })
                    .When(x => x.VATAmount.HasValue && x.VATRate > 0)
                    .WithMessage(item =>
                    {
                        var calculated = item.Amount / (100 + item.VATRate) * item.VATRate;
                        return $"VATAmount ({item.VATAmount}) does not match calculated value ({calculated:F2}), difference exceeds tolerance of {RoundingTolerance}";
                    })
                    .WithErrorCode("VatAmountMismatch");
            });
        }
    }

    public class NegativeAmounts : AbstractValidator<ReceiptRequest>
    {
        public NegativeAmounts()
        {
            When(x => x.cbChargeItems != null
                    && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund)
                    && !x.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void)
                    && !x.IsPartialRefundReceipt(), () =>
            {
                RuleForEach(x => x.cbChargeItems).ChildRules(chargeItem =>
                {
                    chargeItem.RuleFor(x => x.Quantity)
                        .GreaterThanOrEqualTo(0)
                        .When(x => !x.IsDiscount() && !x.IsRefund() && !x.IsVoid())
                        .WithMessage(item => $"Negative quantity ({item.Quantity}) is not allowed for non-refund charge items")
                        .WithErrorCode("NegativeQuantityNotAllowed");

                    chargeItem.RuleFor(x => x.Amount)
                        .GreaterThanOrEqualTo(0)
                        .When(x => !x.IsDiscount() && !x.IsRefund() && !x.IsVoid())
                        .WithMessage(item => $"Negative amount ({item.Amount}) is not allowed for non-refund charge items")
                        .WithErrorCode("NegativeAmountNotAllowed");
                });
            });
        }
    }

    public class DiscountLimit : AbstractValidator<ReceiptRequest>
    {
        public DiscountLimit()
        {
            When(x => x.cbChargeItems != null && x.cbChargeItems.Count > 0, () =>
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

                            var modifiersGrossAmount = modifiers.Sum(x => x.Amount);

                            if (modifiersGrossAmount < 0)
                            {
                                var absoluteDiscountAmount = Math.Abs(modifiersGrossAmount);
                                var absoluteMainItemAmount = Math.Abs(mainItem.Amount);

                                if (absoluteDiscountAmount > absoluteMainItemAmount)
                                {
                                    var mainItemIndex = request.cbChargeItems.IndexOf(mainItem);
                                    context.AddFailure(
                                        "cbChargeItems",
                                        $"Discount ({absoluteDiscountAmount:F2}) exceeds article amount ({absoluteMainItemAmount:F2}) for charge item [{mainItemIndex}] '{mainItem.Description}'");
                                }
                            }
                        }
                    });
            });
        }
    }
}
