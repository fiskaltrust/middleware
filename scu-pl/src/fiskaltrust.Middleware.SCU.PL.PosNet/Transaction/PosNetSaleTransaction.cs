using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// Builds the command sequence of one fiscal sale — trinit → trline… (a sale line, or a storno of
/// one with st1) → trdiscntsubtot? → trpayment… → trend — and enforces the ordering and settlement
/// rules the printer would reject anyway; catching them before any frame is sent keeps a rejected
/// receipt from leaving a half-open transaction on the device. The settlement rule mirrors the
/// device: PAYMENT_METHODS − CHANGE = PAYABLE.
/// </summary>
public class PosNetSaleTransaction
{
    private enum Stage
    {
        NotStarted,
        Initiated,
        HasLines,
        HasSubtotalModifier,
        HasPayments,
        Ended,
    }

    private readonly List<PosNetCommand> _commands = [];
    private Stage _stage = Stage.NotStarted;
    private long _totalGrosze;
    private long _paymentsGrosze;
    private long _changeGrosze;

    public void Begin()
    {
        if (_stage != Stage.NotStarted)
        {
            throw new PLValidationException("The sale transaction was already initiated (trinit must be the first command).");
        }
        _commands.Add(PosNetCommands.Trinit());
        _stage = Stage.Initiated;
    }

    /// <summary>The buyer's NIP may be set at any time inside the open receipt (printed with the footer).</summary>
    public void AddBuyerNip(string buyerNip)
    {
        if (_stage is Stage.NotStarted or Stage.Ended)
        {
            throw new PLValidationException("The buyer's NIP (trnipset) is only valid inside an open receipt.");
        }
        _commands.Add(PosNetCommands.Trnipset(buyerNip));
    }

    /// <summary>
    /// A sale line, optionally with the rabat/narzut granted on it. The line value stays the value
    /// before the modifier — that is what the register prints and measures the discount against —
    /// while the running total follows the value the receipt is settled with.
    /// </summary>
    public void AddLine(string name, int vatSlotIndex, long unitPriceGrosze, decimal quantity, long totalGrosze, PosNetModifier? modifier = null)
    {
        if (_stage is Stage.HasSubtotalModifier)
        {
            // A line sent after the subtotal was discounted would be discounted by neither the
            // rabat od podsumy (the register applied it to the subtotal as it stood) nor anything
            // else, while this class counted it in full — the receipt would only fail at trend,
            // with the transaction already open.
            throw new PLValidationException("A sale line (trline) cannot follow a discount on the subtotal — the subtotal it was granted on would no longer be the receipt's.");
        }
        if (_stage is not (Stage.Initiated or Stage.HasLines))
        {
            throw new PLValidationException("A sale line (trline) is only valid after trinit and before any payment.");
        }
        if (totalGrosze <= 0 || unitPriceGrosze <= 0 || quantity <= 0)
        {
            throw new PLValidationException($"The sale line '{name}' must have a positive price, quantity and amount — returns are separate documents on a Polish register.");
        }
        // The device enforces this too ("the discount may not exceed the value of the goods",
        // POT-I-DEV-05 p.219), but it would enforce it on an open transaction: a line discount that
        // cannot be granted is caught here, before trinit put paper in motion.
        if (modifier is { IsDiscount: true } && modifier.AmountGrosze > totalGrosze)
        {
            throw new PLValidationException(
                $"The discount of {modifier.AmountGrosze.GroszeToPlnText()} on '{name}' exceeds the position's value of {totalGrosze.GroszeToPlnText()} — a Polish register cannot print a negative sale line.");
        }
        _commands.Add(PosNetCommands.Trline(name, vatSlotIndex, unitPriceGrosze, quantity, totalGrosze, modifier));
        _totalGrosze += totalGrosze + (modifier?.SignedAmountGrosze ?? 0);
        _stage = Stage.HasLines;
    }

    /// <summary>
    /// A storno of a position already sold in this receipt: a trline with the reversal flag that
    /// repeats the goods and takes the stated quantity and value off them.
    /// </summary>
    /// <param name="modifier">
    /// The rabat/narzut the reversed position was sold with. It has to travel on the storno as well:
    /// the register then takes the discounted value off the receipt, where a storno without it takes
    /// off the line's value before the rabat.
    /// </param>
    public void AddReversalLine(string name, int vatSlotIndex, long unitPriceGrosze, decimal quantity, long totalGrosze, PosNetModifier? modifier = null)
    {
        if (_stage is Stage.HasSubtotalModifier)
        {
            // Same reason as for a sale line: the subtotal the rabat was granted on would no longer be the receipt's.
            throw new PLValidationException("A storno (trline st1) cannot follow a discount on the subtotal — the subtotal it was granted on would no longer be the receipt's.");
        }
        if (_stage is not Stage.HasLines)
        {
            throw new PLValidationException("A storno (trline st1) is only valid after the sale line it reverses and before any payment.");
        }
        if (totalGrosze <= 0 || unitPriceGrosze <= 0 || quantity <= 0)
        {
            throw new PLValidationException($"The storno of '{name}' must reverse a positive quantity and value — the reversal travels in the st flag, not in a sign.");
        }
        // What the receipt loses is what the reversed position contributed: its value after the
        // rabat/narzut it was sold with, which is why the modifier travels on the storno too.
        var reversedGrosze = totalGrosze + (modifier?.SignedAmountGrosze ?? 0);
        if (reversedGrosze > _totalGrosze)
        {
            throw new PLValidationException(
                $"The storno of '{name}' reverses {reversedGrosze.GroszeToPlnText()}, which is more than the {_totalGrosze.GroszeToPlnText()} this receipt has sold so far.");
        }
        _commands.Add(PosNetCommands.Trline(name, vatSlotIndex, unitPriceGrosze, quantity, totalGrosze, modifier, isReversal: true));
        _totalGrosze -= reversedGrosze;
    }

    /// <summary>
    /// A rabat/narzut od podsumy (trdiscntsubtot): it applies to everything sold so far, so it
    /// belongs after the lines and before the payments.
    /// </summary>
    /// <remarks>
    /// The register distributes the amount over the PTU rates of the receipt itself, proportionally
    /// to the turnover in each (POT-I-DEV-05 p.226). On a receipt that sells in more than one rate,
    /// the per-rate split on the fiscal document is therefore the register's, not the one the POS
    /// tagged the discount position with — that is what a discount on the subtotal is on a Polish
    /// register, and the reason a discount belonging to one position travels on its line instead.
    /// </remarks>
    public void AddSubtotalModifier(PosNetModifier modifier)
    {
        if (_stage is not (Stage.HasLines or Stage.HasSubtotalModifier))
        {
            throw new PLValidationException("A discount on the subtotal (trdiscntsubtot) is only valid after at least one sale line and before any payment.");
        }
        if (modifier.IsDiscount && modifier.AmountGrosze >= _totalGrosze)
        {
            throw new PLValidationException(
                $"The subtotal discount of {modifier.AmountGrosze.GroszeToPlnText()} is not less than the subtotal of {_totalGrosze.GroszeToPlnText()} — a fiscal receipt cannot be printed with a total of zero or less.");
        }
        _commands.Add(PosNetCommands.Trdiscntsubtot(modifier));
        _totalGrosze += modifier.SignedAmountGrosze;
        _stage = Stage.HasSubtotalModifier;
    }

    public void AddPayment(int paymentType, long amountGrosze, bool isChange, string? name = null)
    {
        if (_stage is not (Stage.HasLines or Stage.HasSubtotalModifier or Stage.HasPayments))
        {
            throw new PLValidationException("A payment (trpayment) is only valid after at least one sale line.");
        }
        if (amountGrosze <= 0)
        {
            throw new PLValidationException("A payment amount must be positive (change is marked with the change flag, not a sign).");
        }
        _commands.Add(PosNetCommands.Trpayment(paymentType, amountGrosze, isChange, name));
        if (isChange)
        {
            _changeGrosze += amountGrosze;
        }
        else
        {
            _paymentsGrosze += amountGrosze;
        }
        _stage = Stage.HasPayments;
    }

    public IReadOnlyList<PosNetCommand> End() => End([]);

    /// <summary>
    /// Ends the transaction; with additional lines the receipt is closed with trend fe0, the lines
    /// follow as trftrln and trftrend finishes the printout (POT-I-DEV-37 pp. 274–278).
    /// </summary>
    public IReadOnlyList<PosNetCommand> End(IReadOnlyList<PosNetFooterLine> footerLines)
    {
        if (_stage != Stage.HasPayments)
        {
            throw new PLValidationException("Ending the transaction (trend) requires at least one sale line and one payment.");
        }
        // Discounts can only take the total down, so this is where a receipt discounted to nothing
        // is named as that rather than surfacing as a payment or settlement complaint.
        if (_totalGrosze <= 0)
        {
            throw new PLValidationException(
                $"The receipt totals {_totalGrosze.GroszeToPlnText()} after its discounts — a fiscal document needs a positive total.");
        }
        if (_paymentsGrosze - _changeGrosze != _totalGrosze)
        {
            throw new PLValidationException($"The payments do not settle the receipt: payments ({_paymentsGrosze} gr) minus change ({_changeGrosze} gr) must equal the total ({_totalGrosze} gr).");
        }
        _commands.Add(PosNetCommands.Trend(_totalGrosze, _paymentsGrosze, _changeGrosze, endFooter: footerLines.Count == 0));
        foreach (var line in footerLines)
        {
            _commands.Add(PosNetCommands.Trftrln(line.Text, line.DoubleWidth, line.DoubleHeight));
        }
        if (footerLines.Count > 0)
        {
            _commands.Add(PosNetCommands.Trftrend());
        }
        _stage = Stage.Ended;
        return _commands;
    }
}
