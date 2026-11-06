using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// Counts the sales lines of a receipt: R13 <c>ENC-TIK-LIG-NBR</c>, "Nombre de lignes de vente de
/// la pièce" (R13 v4.2, En-tête row 26). The SCUs sign it (LNE data-set field 15) and the queue
/// prints it (market-fr#148, ADR 0011), so both count with this one rule.
/// </summary>
/// <remarks>
/// A charge item is a sales line unless it is a discount or surcharge (<c>ExtraOrDiscount</c>), a
/// down payment (<c>Downpayment</c>) or a returnable deposit (<c>ReturnableDeposit</c>). In R13 a
/// discount is carried on the line it applies to (<c>ENC-TIK-LIG-REM-*</c>), not as a line of its
/// own; down payments and deposits are not a sale of the goods. Refunded and voided lines still
/// count, because they are lines of the document. The interpretation is recorded on
/// fiskaltrust/market-fr#148.
/// </remarks>
public static class FRSalesLines
{
    private static readonly ChargeItemCaseFlags[] ExcludedFlags =
    [
        ChargeItemCaseFlags.ExtraOrDiscount,
        ChargeItemCaseFlags.Downpayment,
        ChargeItemCaseFlags.ReturnableDeposit,
    ];

    public static int Count(ReceiptRequest request) => (request.cbChargeItems ?? new List<ChargeItem>()).Count(IsSalesLine);

    public static bool IsSalesLine(ChargeItem chargeItem) => !ExcludedFlags.Any(flag => chargeItem.ftChargeItemCase.IsFlag(flag));
}
