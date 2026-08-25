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
/// </summary>
public static class PosNetReceiptMapper
{
    private const int MaxGoodsNameLength = 80;
    private const int MaxPaymentNameLength = 25;

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

        foreach (var chargeItem in request.cbChargeItems ?? [])
        {
            if (chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Void) || chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund))
            {
                throw new PLValidationException("Voided or refunded positions are not supported by the PosNet SCU yet — returns are separate non-fiscal documents on the register.");
            }

            // The queue lets discount and extra positions through because they do not make a
            // document a return. The register expresses them as line- or subtotal-level
            // rabat/narzut parameters rather than as positions of their own, which is a follow-up
            // to middleware#751 — until then they are rejected here, before a transaction is
            // opened, instead of failing as an unexplained negative sale line.
            if (chargeItem.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount))
            {
                throw new PLValidationException($"The discount/extra position '{chargeItem.Description}' is not supported by the PosNet SCU yet — apply the discount to the position amounts and send net line totals.");
            }

            var slot = ptuSlotResolver.Resolve(chargeItem.ftChargeItemCase);
            var totalGrosze = chargeItem.Amount.ToGrosze();
            var quantity = chargeItem.Quantity;

            transaction.AddLine(PosNetText.ToField(chargeItem.Description, MaxGoodsNameLength), ToVatSlotIndex(slot.PtuSlot), ToUnitPriceGrosze(chargeItem.Description, totalGrosze, quantity), quantity, totalGrosze);
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
