using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;
using fiskaltrust.Middleware.SCU.PL.TestSupport.Emulator;

namespace fiskaltrust.Middleware.SCU.PL.TestSupport.Verification;

/// <summary>
/// What a fiscal receipt should leave behind on the register: its value per PTU slot, what was paid
/// and what came back as change. Derived from the <see cref="ReceiptRequest"/> alone — not from
/// the commands the SCU built from it — so that the SCU's mapping is held against an independent
/// reading of the receipt.
/// </summary>
/// <param name="PerRateGrosze">The receipt's value in each PTU slot A–G, after discounts, extras and stornos.</param>
/// <param name="PaymentsGrosze">The sum of the payments.</param>
/// <param name="ChangeGrosze">The change handed out.</param>
/// <param name="SplitsAcrossRates">
/// Whether a rabat/narzut od podsumy had to be split over more than one rate. The register's
/// rounding of that split is not documented (<see cref="PosNetArithmetic.Distribute"/>), so a
/// comparison then allows one grosz of slack per slot while the total stays exact.
/// </param>
public sealed record FiscalFootprint(IReadOnlyList<long> PerRateGrosze, long PaymentsGrosze, long ChangeGrosze, bool SplitsAcrossRates)
{
    public long TotalGrosze => PerRateGrosze.Sum();

    /// <summary>
    /// Reads the footprint off a receipt request, following the receipt model's own rules for which
    /// position a discount/extra or storno refers to: the sale position sharing the integer part of
    /// its <c>Position</c> where the POS set one, otherwise the sale position in front of it. A sale
    /// adds its value to its slot, a discount/extra changes the value of the position it belongs to,
    /// one that belongs to no position is a rabat od podsumy that the register splits over the rates
    /// sold, and a storno takes off what its position contributed — the value after the rabat where
    /// the position was sold with one, whichever of the two values the storno states, because a
    /// discounted position is reversed as a whole and the register repeats the rabat on the reversal.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A discount/extra or storno names a position the receipt does not carry, or a storno has no
    /// position to reverse — a receipt the SCU refuses, which has no footprint.
    /// </exception>
    public static FiscalFootprint Of(ReceiptRequest request, PtuSlotResolver ptuSlotResolver)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(ptuSlotResolver);

        var sales = new List<Sale>();
        var modifiers = new List<Reference>();
        var stornos = new List<Reference>();

        foreach (var item in request.cbChargeItems ?? [])
        {
            if (item.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount))
            {
                modifiers.Add(new Reference(item, sales.Count - 1));
            }
            else if (item.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Void))
            {
                stornos.Add(new Reference(item, sales.Count - 1));
            }
            else
            {
                sales.Add(new Sale(item, SlotOf(item, ptuSlotResolver), item.Amount.ToGrosze()));
            }
        }

        var subtotalModifiersGrosze = new List<long>();
        foreach (var (item, precedingSaleIndex) in modifiers)
        {
            var target = TargetOf(item, precedingSaleIndex, sales);
            if (target < 0)
            {
                subtotalModifiersGrosze.Add(item.Amount.ToGrosze());
            }
            else
            {
                sales[target].ModifierGrosze += item.Amount.ToGrosze();
            }
        }

        // Stornos are read after every modifier has found its position: what a storno takes off is
        // the position's value after its rabat, so the rabat has to be known first.
        foreach (var (item, precedingSaleIndex) in stornos)
        {
            var target = TargetOf(item, precedingSaleIndex, sales);
            if (target < 0)
            {
                throw new InvalidOperationException($"The voided position '{item.Description}' has no sale position to reverse; the receipt has no footprint.");
            }
            var sale = sales[target];
            sale.ReversedGrosze += sale.ModifierGrosze != 0 ? sale.NetGrosze : Math.Abs(item.Amount.ToGrosze());
        }

        var perRate = new long[PosNetDeviceModel.SlotCount];
        foreach (var sale in sales)
        {
            perRate[sale.Slot] += sale.NetGrosze - sale.ReversedGrosze;
        }

        var splitsAcrossRates = false;
        foreach (var modifierGrosze in subtotalModifiersGrosze)
        {
            var shares = PosNetArithmetic.Distribute(perRate, Math.Abs(modifierGrosze));
            splitsAcrossRates |= shares.Count(s => s != 0) > 1;
            for (var i = 0; i < perRate.Length; i++)
            {
                perRate[i] += modifierGrosze < 0 ? -shares[i] : shares[i];
            }
        }

        long payments = 0;
        long change = 0;
        foreach (var payItem in request.cbPayItems ?? [])
        {
            var amountGrosze = payItem.Amount.ToGrosze();
            if (payItem.ftPayItemCase.IsFlag(PayItemCaseFlags.Change) || amountGrosze < 0)
            {
                change += Math.Abs(amountGrosze);
            }
            else
            {
                payments += amountGrosze;
            }
        }

        return new FiscalFootprint(perRate, payments, change, splitsAcrossRates);
    }

    /// <summary>
    /// The sale position a discount/extra or storno refers to, or -1 for none: the one sharing the
    /// integer part of its <c>Position</c>, otherwise the one in front of it. A whole-number position
    /// no sale shares is the POS numbering every charge item in sequence, and reads by order.
    /// </summary>
    private static int TargetOf(ChargeItem item, int precedingSaleIndex, List<Sale> sales)
    {
        if (item.Position == 0m)
        {
            return precedingSaleIndex;
        }
        var position = decimal.Truncate(item.Position);
        var index = sales.FindIndex(sale => sale.Item.Position != 0m && decimal.Truncate(sale.Item.Position) == position);
        if (index >= 0)
        {
            return index;
        }
        if (position == item.Position)
        {
            return precedingSaleIndex;
        }
        throw new InvalidOperationException($"'{item.Description}' is sent on Position {item.Position}, and the receipt carries no sale position {position}; the receipt has no footprint.");
    }

    private static int SlotOf(ChargeItem item, PtuSlotResolver ptuSlotResolver)
        => PosNetReceiptMapper.ToVatSlotIndex(ptuSlotResolver.Resolve(item.ftChargeItemCase).PtuSlot);

    /// <summary>A sale position with what the receipt did to it afterwards.</summary>
    private sealed class Sale(ChargeItem item, int slot, long amountGrosze)
    {
        public ChargeItem Item { get; } = item;

        public int Slot { get; } = slot;

        public long AmountGrosze { get; } = amountGrosze;

        /// <summary>The rabat/narzut granted on it, signed: negative for a discount.</summary>
        public long ModifierGrosze { get; set; }

        /// <summary>How much of it was reversed, in grosze of what it contributed.</summary>
        public long ReversedGrosze { get; set; }

        /// <summary>What the position contributes to the receipt: its value after the rabat/narzut.</summary>
        public long NetGrosze => AmountGrosze + ModifierGrosze;
    }

    /// <summary>A discount/extra or storno with the index of the sale position that arrived before it (-1 for none).</summary>
    private sealed record Reference(ChargeItem Item, int PrecedingSaleIndex);
}

/// <summary>
/// Holds what the register recorded for a receipt against what the receipt should have left behind.
/// Assertion-library neutral on purpose: it returns the discrepancies as sentences, and the test
/// asserts that there are none — so the same comparison serves xunit and the launcher alike.
/// </summary>
public static class FootprintComparer
{
    /// <param name="expected">What the receipt request implies.</param>
    /// <param name="transaction">The <c>strns</c> reading taken right after the receipt.</param>
    /// <param name="before">The snapshot taken before the receipt.</param>
    /// <param name="after">The snapshot taken after the receipt.</param>
    /// <param name="reportedDocumentNumber">The fiscal document number the SCU put into the response, if any.</param>
    /// <param name="verifyTotalizers">
    /// Whether the receipt totalizers (<c>stot pa..pg</c>) are expected to move by the receipt's
    /// value. They do on the device model and on a fiscalized register; whether a non-fiscal
    /// register totalizes its NIEFISKALNY printouts has not been measured yet.
    /// </param>
    /// <returns>One sentence per discrepancy; empty when the register recorded what was sent.</returns>
    public static IReadOnlyList<string> Compare(
        FiscalFootprint expected,
        TransactionReading transaction,
        FiscalSnapshot before,
        FiscalSnapshot after,
        long? reportedDocumentNumber,
        bool verifyTotalizers = true)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var discrepancies = new List<string>();
        var slack = expected.SplitsAcrossRates ? 1 : 0;

        if (transaction.TransactionOpen)
        {
            discrepancies.Add("The register still has a transaction open after the receipt.");
        }
        if (transaction.DocumentType != PosNetDeviceModel.ReceiptDocumentType)
        {
            discrepancies.Add($"The last document on the register is of type {transaction.DocumentType}, not a receipt (16).");
        }

        for (var i = 0; i < PosNetDeviceModel.SlotCount; i++)
        {
            var slot = (char)('A' + i);
            if (Math.Abs(transaction.PerRateGrosze[i] - expected.PerRateGrosze[i]) > slack)
            {
                discrepancies.Add($"PTU {slot}: the receipt was sent for {expected.PerRateGrosze[i].GroszeToPlnText()} but the register printed {transaction.PerRateGrosze[i].GroszeToPlnText()}.");
            }
            if (verifyTotalizers)
            {
                var delta = after.ReceiptTotalizersGrosze[i] - before.ReceiptTotalizersGrosze[i];
                if (Math.Abs(delta - expected.PerRateGrosze[i]) > slack)
                {
                    discrepancies.Add($"PTU {slot}: the totalizer moved by {delta.GroszeToPlnText()} where the receipt was worth {expected.PerRateGrosze[i].GroszeToPlnText()}.");
                }
            }
        }
        if (transaction.TotalGrosze != expected.TotalGrosze)
        {
            discrepancies.Add($"The receipt was sent for {expected.TotalGrosze.GroszeToPlnText()} in total but the register printed {transaction.TotalGrosze.GroszeToPlnText()}.");
        }
        if (transaction.PaymentsGrosze != expected.PaymentsGrosze)
        {
            discrepancies.Add($"Payments of {expected.PaymentsGrosze.GroszeToPlnText()} were sent but the register recorded {transaction.PaymentsGrosze.GroszeToPlnText()}.");
        }
        if (transaction.ChangeGrosze != expected.ChangeGrosze)
        {
            discrepancies.Add($"Change of {expected.ChangeGrosze.GroszeToPlnText()} was sent but the register recorded {transaction.ChangeGrosze.GroszeToPlnText()}.");
        }

        if (after.ReceiptCount != before.ReceiptCount + 1)
        {
            discrepancies.Add($"The receipt counter went from {before.ReceiptCount} to {after.ReceiptCount}; one receipt was sent.");
        }
        if (after.CompletedReceipts != before.CompletedReceipts + 1)
        {
            discrepancies.Add($"The completed-receipts counter went from {before.CompletedReceipts} to {after.CompletedReceipts}; one receipt was sent.");
        }
        if (after.LastReceiptNumber != before.LastReceiptNumber + 1)
        {
            discrepancies.Add($"The last receipt number went from {before.LastReceiptNumber} to {after.LastReceiptNumber}; one receipt was sent.");
        }
        if (after.CanceledCount != before.CanceledCount || after.CanceledTotalGrosze != before.CanceledTotalGrosze)
        {
            discrepancies.Add($"The register canceled something: {before.CanceledCount} → {after.CanceledCount} receipts, {before.CanceledTotalGrosze.GroszeToPlnText()} → {after.CanceledTotalGrosze.GroszeToPlnText()}.");
        }
        if (after.NextDailyReportNumber != before.NextDailyReportNumber)
        {
            discrepancies.Add($"A daily report was made in between ({before.NextDailyReportNumber} → {after.NextDailyReportNumber}).");
        }
        if (reportedDocumentNumber is { } number && number != after.LastReceiptNumber)
        {
            discrepancies.Add($"The response reports fiscal document number {number}, the register's last receipt number is {after.LastReceiptNumber}.");
        }

        return discrepancies;
    }
}
