using System;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// Translates a return receipt (the Refund flag on the receipt case) into the register's goods
/// return. A Polish register has no fiscal document for a return: the positions taken back are
/// recorded in the taxpayer's returns register (ewidencja zwrotów), and the printer documents the
/// amount paid out with a non-fiscal printout (<c>stocash</c>). So the receipt is reduced to that
/// amount — the value of the returned positions — and the payments have to hand exactly it back.
/// </summary>
public static class PosNetReturnMapper
{
    /// <summary>The amount in grosze the register prints as returned, and the command that prints it.</summary>
    public static (long AmountGrosze, PosNetCommand Command) MapReturn(ReceiptRequest request)
    {
        long returnedGrosze = 0;
        foreach (var item in request.cbChargeItems ?? [])
        {
            if (item.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.ExtraOrDiscount) || item.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Void))
            {
                throw new PLValidationException(
                    $"The return carries the discount/void position '{item.Description}'. A goods return on a POSNET register is a single amount — send the positions with the value that is handed back.");
            }
            var amountGrosze = item.Amount.ToGrosze();
            if (amountGrosze > 0 && !item.ftChargeItemCase.IsFlag(ChargeItemCaseFlags.Refund))
            {
                throw new PLValidationException(
                    $"The return carries the sale position '{item.Description}' ({amountGrosze.GroszeToPlnText()}). A Polish register records a return as a document of its own — sell in a receipt, return in a return.");
            }
            returnedGrosze += Math.Abs(amountGrosze);
        }
        if (returnedGrosze == 0)
        {
            throw new PLValidationException("The return carries no returned position — there is nothing for the register to print.");
        }

        var paidBackGrosze = (request.cbPayItems ?? []).Sum(payItem => Math.Abs(payItem.Amount.ToGrosze()));
        if (paidBackGrosze != returnedGrosze)
        {
            throw new PLValidationException(
                $"The return hands back {paidBackGrosze.GroszeToPlnText()} for positions worth {returnedGrosze.GroszeToPlnText()} — a goods return pays out exactly the value of what comes back.");
        }

        return (returnedGrosze, PosNetCommands.Stocash(returnedGrosze));
    }
}
