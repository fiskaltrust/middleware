using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// Translates a fiscal sale ReceiptRequest into the POSNET command sequence. PTU slots come from
/// the configured rate table via <see cref="PtuSlotResolver"/> (the case→rate mapping is
/// statutory, the slot letters are owned by the device); amounts travel as integer grosze.
/// Discount and extra positions are not positions on a Polish register: they become the rabat/narzut
/// of the sale line they belong to — by <c>Position</c> where the POS sets one, otherwise the line
/// in front of them (see <see cref="AssignModifiers"/>) — or a rabat od podsumy when they belong to
/// no line.
/// </summary>
public static class PosNetReceiptMapper
{
    private const int MaxGoodsNameLength = 80;
    private const int MaxPaymentNameLength = 25;

    /// <summary>The rn / na fields of a rabat/narzut carry up to 25 characters (POT-I-DEV-05 p.219).</summary>
    private const int MaxModifierNameLength = 25;

    public static IReadOnlyList<PosNetCommand> MapSale(ReceiptRequest request, PtuSlotResolver ptuSlotResolver)
    {
        var transaction = new PosNetSaleTransaction();
        transaction.Begin();

        // A paragon z NIP: the queue validates that the flag comes with a CustomerVATId; the SCU
        // re-reads it here because the printed NIP is a legal element of the fiscal document.
        if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.ReceiverIsBusiness))
        {
            var buyerNip = GetCustomerVatId(request)
                ?? throw new PLValidationException("A NIP receipt (paragon z NIP) requires the buyer's NIP as CustomerVATId in cbCustomer.");
            transaction.AddBuyerNip(buyerNip);
        }

        // Neither a discount nor a reversal is a position of its own on a register: a rabat/narzut is
        // a parameter of the sale line it belongs to (or one on the subtotal), and a storno is a
        // trline of its own that repeats what was sold with the st flag set. Both name a line that
        // may already have been sent, so the charge items are read into entries first and the
        // relations are resolved before a single command is built (see AssignModifiers and
        // ResolveReversal). Sale lines and stornos keep the order the POS sent them in.
        var entries = new List<ReceiptEntry>();
        var lines = new List<SaleLine>();
        var modifiers = new List<PendingModifier>();

        foreach (var chargeItem in request.cbChargeItems ?? [])
        {
            if (chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund))
            {
                throw new PLValidationException("Refunded positions are not supported by the PosNet SCU yet — a return is a separate document on the register, not a line of this receipt.");
            }

            var isVoid = chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Void);
            var isModifier = chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount);

            if (isVoid && isModifier)
            {
                // A granted rabat/narzut is not undone by a storno: the register has no reversal for
                // one on the subtotal at all (POT-I-DEV-05 p.226), and one on a line is part of that
                // line rather than an operation of its own.
                throw new PLValidationException(
                    $"The voided discount/extra '{chargeItem.Description}' cannot be reversed on a POSNET register — send the position with the rabat/narzut it ends up with.");
            }

            if (isModifier)
            {
                // Which line this belongs to can only be settled once every line is known, so what
                // stands in front of it is remembered here and resolved below: the last sale line,
                // and whether a storno came between the two.
                var precedingReversal = entries.Count > 0 ? entries[^1] as PendingReversal : null;
                modifiers.Add(new PendingModifier(chargeItem, lines.Count - 1, precedingReversal));
                continue;
            }

            if (isVoid)
            {
                entries.Add(new PendingReversal(chargeItem, lines.Count - 1));
                continue;
            }

            var line = new SaleLine(chargeItem, ptuSlotResolver.Resolve(chargeItem.ftChargeItemCase).PtuSlot);
            lines.Add(line);
            entries.Add(line);
        }

        // Every rabat finds its line before the first storno is resolved: a storno of a discounted
        // position repeats that rabat, so the line has to carry it first.
        var subtotalModifiers = AssignModifiers(lines, modifiers);

        foreach (var entry in entries)
        {
            switch (entry)
            {
                case SaleLine line:
                    transaction.AddLine(line.Name, line.PtuSlotIndex, line.UnitPriceGrosze, line.Quantity, line.TotalGrosze, line.Modifier);
                    break;
                case PendingReversal pending:
                    var reversal = ResolveReversal(pending, lines);
                    transaction.AddReversalLine(
                        reversal.Target.Name,
                        reversal.Target.PtuSlotIndex,
                        reversal.Target.UnitPriceGrosze,
                        reversal.Quantity,
                        reversal.TotalGrosze,
                        reversal.Target.Modifier);
                    break;
            }
        }

        foreach (var subtotalModifier in subtotalModifiers)
        {
            transaction.AddSubtotalModifier(subtotalModifier);
        }

        foreach (var payItem in request.cbPayItems ?? [])
        {
            if (payItem.ftPayItemCase.IsFlag(PayItemCaseFlags.Void) || payItem.ftPayItemCase.IsFlag(PayItemCaseFlags.Refund))
            {
                throw new PLValidationException("Voided or refunded payments are not supported by the PosNet SCU yet.");
            }

            // Change flows out of the till and is handed over as a negative amount; on the wire
            // it is a positive deposit with the change flag (re1).
            var isChange = payItem.ftPayItemCase.IsFlag(PayItemCaseFlags.Change) || payItem.Amount < 0;
            var amountGrosze = Math.Abs(payItem.Amount.ToGrosze());
            transaction.AddPayment(ToPaymentType(payItem.ftPayItemCase), amountGrosze, isChange, PosNetText.ToField(payItem.Description, MaxPaymentNameLength));
        }

        // Additional lines the POS asked for (ftReceiptCaseData.PL.printout.lines) close the
        // receipt with trend fe0 and follow it; the footer codes of the same request are a printout
        // configuration the SCU sends before trinit (see PosNetPLSSCD).
        return transaction.End(PosNetPrintoutReader.Read(request)?.Lines ?? []);
    }

    /// <summary>A command this receipt will send for a charge item, in the order the POS sent it.</summary>
    private abstract class ReceiptEntry;

    /// <summary>
    /// A sale position: everything the trline needs, worked out up front so a receipt that cannot be
    /// printed fails before the first command is built rather than half way through the list.
    /// </summary>
    private sealed class SaleLine : ReceiptEntry
    {
        public SaleLine(ChargeItem item, string ptuSlot)
        {
            Item = item;
            Name = PosNetText.ToField(item.Description, MaxGoodsNameLength);
            PtuSlotIndex = ToVatSlotIndex(ptuSlot);
            TotalGrosze = item.Amount.ToGrosze();
            Quantity = item.Quantity;
            UnitPriceGrosze = ToUnitPriceGrosze(item.Description, TotalGrosze, Quantity);
        }

        public ChargeItem Item { get; }

        public string Name { get; }

        public int PtuSlotIndex { get; }

        public long TotalGrosze { get; }

        public decimal Quantity { get; }

        public long UnitPriceGrosze { get; }

        public PosNetModifier? Modifier { get; set; }

        /// <summary>How much of this position has already been reversed, in grosze.</summary>
        public long ReversedGrosze { get; set; }

        /// <summary>The value the position contributes to the receipt: its line value after the rabat/narzut it carries.</summary>
        public long NetGrosze => TotalGrosze + (Modifier?.SignedAmountGrosze ?? 0);
    }

    /// <summary>A voided position as the POS sent it, with the index of the sale line in front of it (-1 for none).</summary>
    private sealed class PendingReversal(ChargeItem item, int precedingLineIndex) : ReceiptEntry
    {
        public ChargeItem Item { get; } = item;

        public int PrecedingLineIndex { get; } = precedingLineIndex;
    }

    /// <summary>
    /// A storno with everything settled: the position it reverses and how much of it. The trline
    /// repeats the target's goods, PTU slot and unit price — and its rabat/narzut, which is what makes
    /// the register take the discounted value off.
    /// </summary>
    private sealed record ResolvedReversal(SaleLine Target, decimal Quantity, long TotalGrosze);

    /// <summary>
    /// A discount/extra position with the index of the sale line that arrived before it (-1 for
    /// none), and the storno that stands between the two where one does.
    /// </summary>
    private sealed record PendingModifier(ChargeItem Item, int PrecedingLineIndex, PendingReversal? PrecedingReversal);

    /// <summary>
    /// Assigns every discount/extra position to the sale line it belongs to, and returns those that
    /// belong to no line — the rabaty od podsumy, in the order the POS sent them.
    /// </summary>
    /// <remarks>
    /// The receipt model expresses the relation in two ways, and both are read here:
    /// <list type="bullet">
    /// <item><c>Position</c>: a modifier shares the integer part of the position it belongs to
    /// (1.1 belongs to position 1), the convention the receipt model already groups by
    /// (scu-gr Helpers/ReceiptRequestExtensions.GetGroupedChargeItemsByPosition). It is the only one
    /// that can name a position that is not the previous one — <c>1 Kawa, 2 Piwo, 1.1 Rabat</c>
    /// discounts the coffee — so it is read first wherever the POS sets it.</item>
    /// <item>Order, for a modifier that carries no position (<c>Position</c> 0 is the model's
    /// "unset"): it belongs to the position in front of it. A modifier with no position in front of
    /// it belongs to no line, and a register has exactly one reading for that — the rabat od
    /// podsumy.</item>
    /// </list>
    /// A whole-number position that no sale line shares is not a reference to a line at all: a POS
    /// that numbers every charge item in sequence gives the modifier the next number, and such a
    /// receipt reads by order like one without positions. A fractional position the receipt does not
    /// carry is a mistake, not a receipt-level discount: the POS said which line it meant. It is
    /// refused rather than quietly widened to the whole receipt.
    /// </remarks>
    private static List<PosNetModifier> AssignModifiers(List<SaleLine> lines, List<PendingModifier> modifiers)
    {
        var subtotalModifiers = new List<PosNetModifier>();
        foreach (var (item, precedingLineIndex, precedingReversal) in modifiers)
        {
            var modifier = ToModifier(item);

            var targetIndex = precedingLineIndex;
            var readByOrder = true;
            if (item.Position != 0m)
            {
                var namedIndex = FindLineByPosition(item.Position, lines);
                if (namedIndex >= 0)
                {
                    targetIndex = namedIndex;
                    readByOrder = false;
                }
                else if (item.Position != decimal.Truncate(item.Position))
                {
                    throw new PLValidationException(
                        $"The discount/extra '{item.Description}' is sent on Position {item.Position} and so applies to sale position {decimal.Truncate(item.Position)}, which this receipt does not carry. Send it on the position it belongs to, or without a position to have it apply to the subtotal.");
                }
            }

            if (readByOrder && precedingReversal is not null && ReversalTargetIndex(precedingReversal, lines) == targetIndex)
            {
                // The entry in front of it is a storno, which carries no rabat/narzut of its own, and
                // the line it reversed is the one this modifier would read by order. Attaching the
                // modifier there would discount a position the customer is not paying for. A storno
                // that named another position leaves the line in front intact, and that line takes
                // its discount like any other.
                throw new PLValidationException(
                    $"The discount/extra '{item.Description}' follows the storno of the position it would apply to, and a storno carries no rabat/narzut of its own. Send it before the storno, after the position it belongs to — or name that position in Position.");
            }
            if (targetIndex < 0)
            {
                // Its PTU rate is not read: the register distributes a subtotal discount over the
                // rates of the receipt itself, and a rate sent alongside would not change that.
                subtotalModifiers.Add(modifier);
                continue;
            }

            var line = lines[targetIndex];
            if (line.Modifier is not null)
            {
                throw new PLValidationException(
                    $"The position '{line.Item.Description}' carries more than one discount/extra ('{item.Description}'): a POSNET sale line has room for a single rabat/narzut. Send them as one discount position, or split the sale into a position per discount.");
            }

            // A line discount is granted at the rate of the line — the trline rabat has no rate of
            // its own. A discount booked at a different VAT rate than the position it applies to
            // would therefore print and totalize under the position's rate, changing the tax the POS
            // booked, so it is refused instead. A discount that carries no rate at all is not that
            // case: it is the POS leaving the rate to the position, which is what the register does
            // anyway. The comparison is made on the VAT case rather than on the resolved PTU slot,
            // so a modifier tagged with a rate that has no Polish slot is reported as the mismatch
            // it is instead of as an unresolvable rate table.
            var modifierVatCase = item.ftChargeItemCase.Vat();
            var lineVatCase = line.Item.ftChargeItemCase.Vat();
            if (modifierVatCase != ChargeItemCase.UnknownService && modifierVatCase != lineVatCase)
            {
                throw new PLValidationException(
                    $"The discount/extra '{item.Description}' is booked on the VAT case {modifierVatCase}, but the position it applies to ('{line.Item.Description}') sells on {lineVatCase}. A POSNET line discount is granted at the position's rate — send it with the same VAT rate as the position, or without one.");
            }
            line.Modifier = modifier;
        }
        return subtotalModifiers;
    }

    /// <summary>
    /// Works out which position a storno reverses, and how much of it. The goods name, PTU slot and
    /// unit price travel from the position that was sold — the register matches a reversal against
    /// what it printed and answers 2851/2852 when the quantity or the value does not fit — while how
    /// much is reversed comes from the storno position itself, so a partial storno is possible.
    /// Which position that is, is read the way a discount's is (see <see cref="AssignModifiers"/>):
    /// by <c>Position</c> where the POS set one that a sale line shares, otherwise the line in front.
    /// </summary>
    /// <remarks>
    /// The amount is read as a magnitude: the direction is in the Void flag, not in the sign, which
    /// is how the other SCUs read a voided position as well (scu-it CustomRTServerMapping.GetGrossAmount,
    /// EpsonRTServerMapping.LineNetSign). A POS that sends the storno with either sign is understood.
    /// </remarks>
    private static ResolvedReversal ResolveReversal(PendingReversal reversal, List<SaleLine> lines)
    {
        var item = reversal.Item;

        var targetIndex = ReversalTargetIndex(reversal, lines);
        if (item.Position != 0m
            && item.Position != decimal.Truncate(item.Position)
            && FindLineByPosition(item.Position, lines) < 0)
        {
            throw new PLValidationException(
                $"The voided position '{item.Description}' is sent on Position {item.Position} and so reverses sale position {decimal.Truncate(item.Position)}, which this receipt does not carry. Send it on the position it reverses, or without a position to reverse the position in front of it.");
        }
        if (targetIndex < 0)
        {
            throw new PLValidationException(
                $"The voided position '{item.Description}' has no sale position to reverse. A storno repeats a position the register already printed — send it after the position it reverses, or name that position in Position.");
        }
        if (targetIndex > reversal.PrecedingLineIndex)
        {
            // The lines are sent in the order they arrive, so this one is not on the paper yet.
            throw new PLValidationException(
                $"The voided position '{item.Description}' reverses the sale position '{lines[targetIndex].Item.Description}', which this receipt sells after it. A position can only be reversed once it has been sold.");
        }

        var target = lines[targetIndex];

        // The reversal travels at the target's PTU slot, so a storno the POS booked at another rate
        // would silently move turnover between rates — refused, the way a rabat at another rate is.
        // A storno without a rate leaves it to the position, as a rabat without one does.
        var stornoVatCase = item.ftChargeItemCase.Vat();
        var targetVatCase = target.Item.ftChargeItemCase.Vat();
        if (stornoVatCase != ChargeItemCase.UnknownService && stornoVatCase != targetVatCase)
        {
            throw new PLValidationException(
                $"The voided position '{item.Description}' is booked on the VAT case {stornoVatCase}, but the position it reverses ('{target.Item.Description}') sells on {targetVatCase}. A POSNET storno is printed and totalized at the rate of the position it reverses — send it with the same VAT rate as the position, or without one.");
        }

        var totalGrosze = Math.Abs(item.Amount.ToGrosze());
        if (totalGrosze == 0)
        {
            throw new PLValidationException($"The voided position '{item.Description}' carries no amount — there is nothing for the register to reverse.");
        }

        if (target.Modifier is not null)
        {
            // A position sold with a rabat is reversed by a storno that carries the same rabat: the
            // register then takes the discounted value off, while a storno without it takes off the
            // line's value before the rabat and leaves the receipt totalling less than the positions
            // still on it. Measured on a POSNET THERMAL XL2 ONLINE — with the rabat repeated, the
            // register verified a fiscal value reduced by the discounted amount and refused the
            // other candidate (2805 ERR_ENDTOT_VERIFY).
            if (totalGrosze != target.TotalGrosze && totalGrosze != target.NetGrosze)
            {
                // Part of a discounted position cannot be reversed: the rabat is one amount for the
                // whole line, and how the register would split it over part of one is not documented.
                throw new PLValidationException(
                    $"The storno of '{item.Description}' reverses {totalGrosze.GroszeToPlnText()} of the sale position '{target.Item.Description}', which was sold with a rabat/narzut and can only be reversed as a whole — state {target.TotalGrosze.GroszeToPlnText()} before it or {target.NetGrosze.GroszeToPlnText()} after it.");
            }
            if (target.ReversedGrosze > 0)
            {
                throw new PLValidationException(
                    $"The sale position '{target.Item.Description}' has already been reversed; a position sold with a rabat/narzut is reversed once, as a whole.");
            }

            target.ReversedGrosze = target.TotalGrosze;
            return new ResolvedReversal(target, target.Quantity, target.TotalGrosze);
        }

        if (target.ReversedGrosze + totalGrosze > target.TotalGrosze)
        {
            throw new PLValidationException(
                $"The storno of '{item.Description}' reverses {totalGrosze.GroszeToPlnText()} of the sale position '{target.Item.Description}', which is more than the {(target.TotalGrosze - target.ReversedGrosze).GroszeToPlnText()} still standing on it.");
        }

        // How much is reversed is the amount, and price x quantity has to hold on a reversal line as
        // it does on a sale line — the register verifies both (2851/2852) — so the quantity follows
        // from the amount and the unit price the position was printed with. The target is always
        // already on the paper here (checked above), so its unit price has passed AddLine's
        // positivity guard and the division is safe.
        //
        // A quantity the POS sends cannot be told apart from the receipt model's default of 1, so it
        // is read as a cross-check rather than as the source: a stated quantity other than 1 that
        // does not fit the amount is a POS error worth naming, while a storno of three items sent
        // with the default quantity still reverses all three.
        var quantity = totalGrosze / (decimal)target.UnitPriceGrosze;
        if (target.UnitPriceGrosze * quantity != totalGrosze)
        {
            throw new PLValidationException(
                $"The storno of '{item.Description}' reverses {totalGrosze.GroszeToPlnText()} of the sale position '{target.Item.Description}', which does not divide by the unit price of {target.UnitPriceGrosze.GroszeToPlnText()} it was printed with — a storno reverses a quantity of that position, and the register verifies price x quantity against the line value (errors 2851/2852).");
        }
        var statedQuantity = Math.Abs(item.Quantity);
        if (statedQuantity is not (0m or 1m) && statedQuantity != quantity)
        {
            throw new PLValidationException(
                $"The storno of '{item.Description}' states quantity {statedQuantity.ToString(CultureInfo.InvariantCulture)} but reverses {totalGrosze.GroszeToPlnText()}, which is {quantity.ToString(CultureInfo.InvariantCulture)} at the unit price of {target.UnitPriceGrosze.GroszeToPlnText()} the sale position was printed with — the register verifies both (errors 2851/2852). Send the amount that belongs to the quantity, or the amount alone.");
        }
        RequireWireQuantity($"The storno of '{item.Description}'", quantity);

        target.ReversedGrosze += totalGrosze;
        return new ResolvedReversal(target, quantity, totalGrosze);
    }

    /// <summary>
    /// Which sale line a storno reverses, read the way a modifier's target is (see
    /// <see cref="AssignModifiers"/>): the line whose position it names, otherwise the line in front
    /// of it. A named position the receipt does not carry reads as the line in front here — whether
    /// that is a mistake is settled by <see cref="ResolveReversal"/>, the one place that refuses a
    /// storno.
    /// </summary>
    private static int ReversalTargetIndex(PendingReversal reversal, List<SaleLine> lines)
    {
        var namedIndex = reversal.Item.Position != 0m ? FindLineByPosition(reversal.Item.Position, lines) : -1;
        return namedIndex >= 0 ? namedIndex : reversal.PrecedingLineIndex;
    }

    /// <summary>The sale line whose position shares the integer part of <paramref name="position"/>, or -1 when none does.</summary>
    private static int FindLineByPosition(decimal position, List<SaleLine> lines)
    {
        var wholePart = decimal.Truncate(position);
        return lines.FindIndex(line => line.Item.Position != 0m && decimal.Truncate(line.Item.Position) == wholePart);
    }

    /// <summary>
    /// Reads a discount/extra position into a <see cref="PosNetModifier"/>. The direction comes from
    /// the sign the receipt model gives a modifier position — negative is a rabat, positive a narzut
    /// (the queue's ChargeItemExtensions.IsDiscount/IsExtra read it the same way). Void and refund
    /// positions never reach here, so the sign alone decides.
    /// </summary>
    private static PosNetModifier ToModifier(ChargeItem chargeItem)
    {
        var amountGrosze = chargeItem.Amount.ToGrosze();
        if (amountGrosze == 0)
        {
            throw new PLValidationException(
                $"The discount/extra position '{chargeItem.Description}' carries no amount — a rabat/narzut of 0.00 gr is nothing the register can print.");
        }
        return new PosNetModifier(amountGrosze < 0, Math.Abs(amountGrosze), PosNetText.ToField(chargeItem.Description, MaxModifierNameLength));
    }

    /// <summary>
    /// The trline unit price (<c>pr</c>) in grosze. A POSNET sale line carries price, quantity and
    /// value, and the three have to agree — the register prints and totalizes all of them, so a
    /// price rounded independently of the line total would put a contradiction on a fiscal
    /// document (10.00 over quantity 3 as 3 × 333 gr = 9.99). Grosze are the smallest unit the
    /// protocol has, so a line total that is not divisible by its quantity cannot be expressed as
    /// one line and is rejected here, before any frame is sent.
    /// </summary>
    public static long ToUnitPriceGrosze(string? description, long totalGrosze, decimal quantity)
    {
        if (quantity == 1m)
        {
            return totalGrosze;
        }
        if (quantity <= 0m || totalGrosze <= 0)
        {
            // Not a sale line at all — the transaction reports that, with the reason.
            return totalGrosze;
        }

        RequireWireQuantity($"The sale line '{description}'", quantity);

        var unitPriceGrosze = totalGrosze / quantity;
        if (unitPriceGrosze != decimal.Truncate(unitPriceGrosze))
        {
            throw new PLValidationException(
                $"The sale line '{description}' cannot be printed by a Polish register: the amount {totalGrosze.GroszeToPlnText()} over quantity {quantity.ToString(CultureInfo.InvariantCulture)} is not a whole number of grosze per unit "
                + "(price × quantity must equal the line value on a fiscal document). Split the position or send an amount that divides by the quantity.");
        }
        return (long) unitPriceGrosze;
    }

    /// <summary>
    /// Every check of price x quantity = value has to be made against the quantity the printer will
    /// see, not the one worked out here: trline carries it with <see cref="PosNetCommands.QuantityDecimals"/>
    /// places, so a quantity with more of them would satisfy the invariant here and violate it on
    /// paper (1.2345 travels as 1.235, and 1.235 x 20.00 is 24.70 against a line value of 24.69) —
    /// with the transaction already open on the device.
    /// </summary>
    /// <param name="subject">Who the quantity belongs to, as the start of the message ("The sale line 'Woda'").</param>
    private static void RequireWireQuantity(string subject, decimal quantity)
    {
        if (decimal.Round(quantity, PosNetCommands.QuantityDecimals, MidpointRounding.AwayFromZero) != quantity)
        {
            throw new PLValidationException(
                $"{subject} cannot be printed by a Polish register: the quantity {quantity.ToString(CultureInfo.InvariantCulture)} has more than "
                + $"{PosNetCommands.QuantityDecimals} decimal places, which the protocol cannot carry. Round the quantity, or split the position.");
        }
    }

    /// <summary>The trline vt parameter is the zero-based PTU slot index: A=0 … G=6.</summary>
    public static int ToVatSlotIndex(string ptuSlot)
    {
        if (ptuSlot.Length != 1 || ptuSlot[0] is < 'A' or > 'G')
        {
            throw new PLValidationException($"'{ptuSlot}' is not a valid PTU slot letter (A–G).");
        }
        return ptuSlot[0] - 'A';
    }

    /// <summary>Maps the ftPayItemCase payment type to the POSNET trpayment ty value.</summary>
    public static int ToPaymentType(PayItemCase payItemCase) => (PayItemCase)((long)payItemCase & 0xFF) switch
    {
        PayItemCase.UnknownPaymentType => 0,
        PayItemCase.CashPayment => 0,
        PayItemCase.CrossedCheque => 3,
        PayItemCase.DebitCardPayment => 2,
        PayItemCase.CreditCardPayment => 2,
        PayItemCase.VoucherPaymentCouponVoucherByMoneyValue => 7,
        PayItemCase.OnlinePayment => 8,
        PayItemCase.AccountsReceivable => 5,
        PayItemCase.SEPATransfer => 8,
        PayItemCase.OtherBankTransfer => 8,
        _ => 6,
    };

    /// <summary>
    /// The IDZ limit of the printer: an e-receipt customer identifier is at most 128 alphanumeric
    /// characters (e.g. the KID from the MF e-Paragony app or a hub-specific customer id).
    /// </summary>
    public const int MaxEReceiptCustomerIdLength = 128;

    /// <summary>
    /// Reads CustomerVATId from cbCustomer (MiddlewareCustomer shape) without referencing the
    /// queue assemblies. The trnipset ni parameter is numeric, so formatting characters
    /// (e.g. "123-456-32-18") are stripped.
    /// </summary>
    private static string? GetCustomerVatId(ReceiptRequest request)
    {
        var value = GetCustomerField(request, "CustomerVATId");
        if (value is null)
        {
            return null;
        }
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    /// <summary>
    /// Reads the e-receipt customer identifier (IDZ) from cbCustomer — the well-known key
    /// <c>eReceiptCustomerId</c> in the generic customer payload (middleware#764). A present
    /// identifier is validated here, before any frame is sent: the printer limits the IDZ to
    /// <see cref="MaxEReceiptCustomerIdLength"/> characters, and the protocol field carries ASCII
    /// only. Absent, empty or unreadable cbCustomer means no binding — a plain paper receipt.
    /// </summary>
    public static string? GetEReceiptCustomerId(ReceiptRequest request)
    {
        var customerId = GetCustomerField(request, "eReceiptCustomerId");
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return null;
        }
        if (customerId.Length > MaxEReceiptCustomerIdLength)
        {
            throw new PLValidationException(
                $"The e-receipt customer identifier (eReceiptCustomerId) is {customerId.Length} characters long — the printer's IDZ limit is {MaxEReceiptCustomerIdLength}.");
        }
        if (customerId.Any(c => c is < ' ' or > '~'))
        {
            throw new PLValidationException(
                "The e-receipt customer identifier (eReceiptCustomerId) contains non-ASCII or control characters, which the IDZ protocol field cannot carry.");
        }
        return customerId;
    }

    /// <summary>Reads one string property of the cbCustomer JSON object, tolerating any other shape.</summary>
    private static string? GetCustomerField(ReceiptRequest request, string fieldName)
    {
        var cbCustomer = request.cbCustomer?.ToString();
        if (string.IsNullOrWhiteSpace(cbCustomer))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(cbCustomer);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }
}
