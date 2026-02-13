using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;

public class ChargeItemValidations : AbstractValidator<ReceiptRequest>
{
    public ChargeItemValidations()
    {
        Include(new DescriptionMinLength());
        Include(new PosReceiptNetAmountLimit());
        Include(new OtherServiceNetAmountLimit());
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
}
