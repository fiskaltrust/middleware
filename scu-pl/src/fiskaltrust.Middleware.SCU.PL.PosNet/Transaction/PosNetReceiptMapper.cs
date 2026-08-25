using System;
using System.Collections.Generic;
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
/// of the sale line they follow, or a rabat od podsumy when they follow none.
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

        // A register has no position of its own for a discount: a rabat/narzut is either a parameter
        // of the sale line it belongs to, or one on the subtotal. Which one a discount position is
        // follows from where it stands. That a modifier belongs to the position in front of it is
        // the convention the shared receipt model already groups by
        // (v2 Helpers/ReceiptRequestExtensions.GetGroupedChargeItems). What that helper does with a
        // modifier standing in front of every position differs — it makes it an entry of its own —
        // because it has nowhere to put it; here it becomes the rabat od podsumy, the only reading a
        // register has for a discount that belongs to no line. A POS that sends its discounts ahead
        // of the positions they apply to therefore has them applied to the receipt, not to a line.
        // The line is held back until the item after it is known.
        ChargeItem? pendingLine = null;
        string? pendingSlot = null;
        PosNetModifier? pendingModifier = null;
        var subtotalModifiers = new List<PosNetModifier>();

        void SendPendingLine()
        {
            if (pendingLine is null)
            {
                return;
            }

            var totalGrosze = pendingLine.Amount.ToGrosze();
            var quantity = pendingLine.Quantity;
            transaction.AddLine(
                PosNetText.ToField(pendingLine.Description, MaxGoodsNameLength),
                ToVatSlotIndex(pendingSlot!),
                ToUnitPriceGrosze(pendingLine.Description, totalGrosze, quantity),
                quantity,
                totalGrosze,
                pendingModifier);
            pendingLine = null;
            pendingSlot = null;
            pendingModifier = null;
        }

        foreach (var chargeItem in request.cbChargeItems ?? [])
        {
            if (chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Void) || chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund))
            {
                throw new PLValidationException("Voided or refunded positions are not supported by the PosNet SCU yet — returns are separate non-fiscal documents on the register.");
            }

            if (chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount))
            {
                var modifier = ToModifier(chargeItem);
                if (pendingLine is null)
                {
                    // No position in front of it: a rabat od podsumy. Its PTU rate is not read —
                    // the register distributes a subtotal discount over the rates of the receipt
                    // itself, and a rate sent alongside would not change that.
                    subtotalModifiers.Add(modifier);
                    continue;
                }
                if (pendingModifier is not null)
                {
                    throw new PLValidationException(
                        $"The position '{pendingLine.Description}' carries more than one discount/extra ('{chargeItem.Description}'): a POSNET sale line has room for a single rabat/narzut. Send them as one discount position, or split the sale into a position per discount.");
                }

                // A line discount is granted at the rate of the line — the trline rabat has no rate
                // of its own. A discount booked at a different VAT rate than the position it applies
                // to would therefore print and totalize under the position's rate, changing the tax
                // the POS booked, so it is refused instead. A discount that carries no rate at all
                // is not that case: it is the POS leaving the rate to the position, which is what
                // the register does anyway. The comparison is made on the VAT case rather than on
                // the resolved PTU slot, so a modifier tagged with a rate that has no Polish slot
                // is reported as the mismatch it is instead of as an unresolvable rate table.
                var modifierVatCase = chargeItem.ftChargeItemCase.Vat();
                var lineVatCase = pendingLine.ftChargeItemCase.Vat();
                if (modifierVatCase != ChargeItemCase.UnknownService && modifierVatCase != lineVatCase)
                {
                    throw new PLValidationException(
                        $"The discount/extra '{chargeItem.Description}' is booked on the VAT case {modifierVatCase}, but the position it applies to ('{pendingLine.Description}') sells on {lineVatCase}. A POSNET line discount is granted at the position's rate — send it with the same VAT rate as the position, or without one.");
                }
                pendingModifier = modifier;
                continue;
            }

            SendPendingLine();
            pendingLine = chargeItem;
            pendingSlot = ptuSlotResolver.Resolve(chargeItem.ftChargeItemCase).PtuSlot;
        }

        SendPendingLine();

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

        return transaction.End();
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

        // The check below has to be made against the quantity the printer will see, not the one we
        // were handed: trline carries it with PosNetCommands.QuantityDecimals places, so a quantity
        // with more of them would satisfy price x quantity = value here and violate it on paper
        // (1.2345 travels as 1.235, and 1.235 x 20.00 is 24.70 against a line value of 24.69).
        if (decimal.Round(quantity, PosNetCommands.QuantityDecimals, MidpointRounding.AwayFromZero) != quantity)
        {
            throw new PLValidationException(
                $"The sale line '{description}' cannot be printed by a Polish register: the quantity {quantity} has more than "
                + $"{PosNetCommands.QuantityDecimals} decimal places, which the protocol cannot carry. Round the quantity, or split the position.");
        }

        var unitPriceGrosze = totalGrosze / quantity;
        if (unitPriceGrosze != decimal.Truncate(unitPriceGrosze))
        {
            throw new PLValidationException(
                $"The sale line '{description}' cannot be printed by a Polish register: the amount {totalGrosze.GroszeToPln()} over quantity {quantity} is not a whole number of grosze per unit "
                + "(price × quantity must equal the line value on a fiscal document). Split the position or send an amount that divides by the quantity.");
        }
        return (long) unitPriceGrosze;
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
    /// Reads CustomerVATId from cbCustomer (MiddlewareCustomer shape) without referencing the
    /// queue assemblies. The trnipset ni parameter is numeric, so formatting characters
    /// (e.g. "123-456-32-18") are stripped.
    /// </summary>
    private static string? GetCustomerVatId(ReceiptRequest request)
    {
        var cbCustomer = request.cbCustomer?.ToString();
        if (string.IsNullOrWhiteSpace(cbCustomer))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(cbCustomer);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name.Equals("CustomerVATId", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                {
                    var digits = new string(property.Value.GetString()!.Where(char.IsDigit).ToArray());
                    return digits.Length == 0 ? null : digits;
                }
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }
}
