using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Validation.Rules.Global;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;

/// <summary>
/// The shared balance, VAT and charge-item description rules skip a handwritten receipt (flag 0x0008). France keeps
/// them: the receipt is entered afterwards from its paper original but enters the grand totals like any other
/// (market-fr#221). The shared rules run unchanged on the receipt read without its handwritten flag,
/// so their codes, messages and property names are the ones a typed receipt gets.
/// </summary>
public class HandwrittenReceiptChecksFR : AbstractValidator<ReceiptRequest>
{
    private static readonly IValidator<ReceiptRequest>[] Rules =
    [
        new ReceiptValidations.ReceiptBalance(),
        new ChargeItemValidations.MandatoryFields(),
        new ChargeItemValidations.VatCalculation(),
        new ChargeItemValidations.ZeroVatRateMustHaveNature(ReceiptValidationsFR.IsDrawerMoveChargeItem),
    ];

    public HandwrittenReceiptChecksFR()
    {
        RuleFor(x => x)
            .Custom((request, context) =>
            {
                var typed = AsTyped(request);
                foreach (var rule in Rules)
                {
                    foreach (var failure in rule.Validate(typed).Errors)
                    {
                        context.AddFailure(failure);
                    }
                }
            })
            .When(x => x.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten));
    }

    /// <summary>
    /// The receipt without its handwritten flag. Only what the four rules read is carried over: the case and the
    /// charge and pay items (the item lists are shared, not copied).
    /// </summary>
    private static ReceiptRequest AsTyped(ReceiptRequest request) => new()
    {
        ftReceiptCase = (ReceiptCase) ((long) request.ftReceiptCase & ~(long) ReceiptCaseFlags.HandWritten),
        Currency = request.Currency,
        cbChargeItems = request.cbChargeItems,
        cbPayItems = request.cbPayItems,
    };
}
