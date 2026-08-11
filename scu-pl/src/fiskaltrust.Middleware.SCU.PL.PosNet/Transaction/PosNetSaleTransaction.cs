using System.Collections.Generic;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// Builds the command sequence of one fiscal sale (trinit → trline… → trpayment… → trend) and
/// enforces the ordering and settlement rules the printer would reject anyway — catching them
/// before any frame is sent keeps a rejected receipt from leaving a half-open transaction on the
/// device. The settlement rule mirrors the device: PAYMENT_METHODS − CHANGE = PAYABLE.
/// </summary>
public class PosNetSaleTransaction
{
    private enum Stage
    {
        NotStarted,
        Initiated,
        HasLines,
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

    public void AddLine(string name, int vatSlotIndex, long unitPriceGrosze, decimal quantity, long totalGrosze)
    {
        if (_stage is not (Stage.Initiated or Stage.HasLines))
        {
            throw new PLValidationException("A sale line (trline) is only valid after trinit and before any payment.");
        }
        if (totalGrosze <= 0 || unitPriceGrosze <= 0 || quantity <= 0)
        {
            throw new PLValidationException($"The sale line '{name}' must have a positive price, quantity and amount — returns are separate documents on a Polish register.");
        }
        _commands.Add(PosNetCommands.Trline(name, vatSlotIndex, unitPriceGrosze, quantity, totalGrosze));
        _totalGrosze += totalGrosze;
        _stage = Stage.HasLines;
    }

    public void AddPayment(int paymentType, long amountGrosze, bool isChange, string? name = null)
    {
        if (_stage is not (Stage.HasLines or Stage.HasPayments))
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

    public IReadOnlyList<PosNetCommand> End()
    {
        if (_stage != Stage.HasPayments)
        {
            throw new PLValidationException("Ending the transaction (trend) requires at least one sale line and one payment.");
        }
        if (_paymentsGrosze - _changeGrosze != _totalGrosze)
        {
            throw new PLValidationException($"The payments do not settle the receipt: payments ({_paymentsGrosze} gr) minus change ({_changeGrosze} gr) must equal the total ({_totalGrosze} gr).");
        }
        _commands.Add(PosNetCommands.Trend(_totalGrosze, _paymentsGrosze, _changeGrosze));
        _stage = Stage.Ended;
        return _commands;
    }
}
