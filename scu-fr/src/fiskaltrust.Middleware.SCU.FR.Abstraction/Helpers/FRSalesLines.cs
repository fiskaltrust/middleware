using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// Counts the sales lines of a receipt: R13 <c>ENC-TIK-LIG-NBR</c>, "Nombre de lignes de vente de
/// la pièce" (R13 v4.2, En-tête row 26). The SCUs sign it (LNE data-set field 15), the queue prints it
/// ("Nombre de lignes") and the archive writes it (<c>LineCount</c>, <c>CountsAsLine</c>) with this one
/// rule (market-fr#148, ADR 0011).
/// </summary>
/// <remarks>
/// <para>
/// A charge item is a sales line unless it is a discount or surcharge (<c>ExtraOrDiscount</c>), a
/// down payment (<c>Downpayment</c>) or a returnable deposit (<c>ReturnableDeposit</c>). In R13 a
/// discount is carried on the line it applies to (<c>ENC-TIK-LIG-REM-*</c>), not as a line of its
/// own; down payments and deposits are not a sale of the goods. Refunded and voided lines still
/// count, because they are lines of the document.
/// </para>
/// <para>
/// With the receipt-case flag "group by position number" (0x0800_0000) set on a receipt that sells, a composite
/// article counts as one line (R13 note 7, market-fr#221): the sales lines are grouped by the integer part of
/// their <c>Position</c>, and each group counts once. A sales line at <c>Position</c> 0 has no position and counts
/// alone; a group of excluded items only does not count; duplicate positions are not refused. On a zero receipt and
/// on the closings (0x2000–0x2013) the same bit means something else and is not read. Without the flag every sales
/// line counts.
/// </para>
/// </remarks>
public static class FRSalesLines
{
    /// <summary>
    /// The receipt-case flag "group by position number". A local constant until the interface names it
    /// <c>GroupByPositionNumber</c> (fiskaltrust.interface 1.3.80).
    /// </summary>
    public const long GroupByPositionNumber = 0x0000_0000_0800_0000;

    private const long ZeroReceipt = 0x2000;
    private const long YearlyClosing = 0x2013;

    private static readonly ChargeItemCaseFlags[] ExcludedFlags =
    [
        ChargeItemCaseFlags.ExtraOrDiscount,
        ChargeItemCaseFlags.Downpayment,
        ChargeItemCaseFlags.ReturnableDeposit,
    ];

    /// <summary>The number of sales lines, as signed, printed and archived.</summary>
    public static int Count(ReceiptRequest request) => CountingLines(request).Count(x => x);

    /// <summary>
    /// For each charge item, in request order, whether it is the one that counts its line: every sales line without
    /// grouping; with grouping, a sales line at <c>Position</c> 0 and the first sales line of each position group.
    /// The number of <c>true</c> values is <see cref="Count"/>.
    /// </summary>
    public static IReadOnlyList<bool> CountingLines(ReceiptRequest request)
    {
        var items = request.cbChargeItems ?? new List<ChargeItem>();
        if (!GroupsByPosition(request))
        {
            return items.Select(IsSalesLine).ToList();
        }

        var countedGroups = new HashSet<decimal>();
        return items.Select(item => IsSalesLine(item) && (item.Position == 0 || countedGroups.Add(decimal.Truncate(item.Position)))).ToList();
    }

    public static bool IsSalesLine(ChargeItem chargeItem) => !ExcludedFlags.Any(flag => chargeItem.ftChargeItemCase.IsFlag(flag));

    private static bool GroupsByPosition(ReceiptRequest request)
    {
        var receiptCase = (long) request.ftReceiptCase;
        var type = receiptCase & 0xFFFF;
        return (receiptCase & GroupByPositionNumber) != 0 && !(type >= ZeroReceipt && type <= YearlyClosing);
    }
}
