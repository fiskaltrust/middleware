using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// The late-signing mode of a French queue (market-fr#231): opened by the first signed receipt the
/// POS re-sends with <c>ReceiptCaseFlags.LateSigning</c> after it could not reach the middleware,
/// closed by the next signed zero receipt. The range lives in the v1 <c>UsedFailed*</c> columns of
/// ftQueueFR and is stored with the chain (market-fr ADR 0012), so it survives a restart.
/// </summary>
public class FRLateSigningState
{
    private readonly ftQueueFR _queueFR;

    public FRLateSigningState(ftQueueFR queueFR)
    {
        _queueFR = queueFR;
    }

    /// <summary>True while late-signed receipts have been signed and no zero receipt has closed the range.</summary>
    public bool IsOpen => _queueFR.UsedFailedCount > 0;

    /// <summary>
    /// Adds a signed late-signed receipt to the range and returns true if it opened the mode. The
    /// range spans the moments the POS issued the receipts (<c>cbReceiptMoment</c>), not the
    /// moments they were signed.
    /// </summary>
    public bool Add(Guid queueItemId, DateTime cbReceiptMoment)
    {
        var opened = !IsOpen;
        if (opened)
        {
            _queueFR.UsedFailedQueueItemId = queueItemId;
            _queueFR.UsedFailedMomentMin = cbReceiptMoment;
            _queueFR.UsedFailedMomentMax = cbReceiptMoment;
        }
        else
        {
            if (_queueFR.UsedFailedMomentMin is null || cbReceiptMoment < _queueFR.UsedFailedMomentMin)
            {
                _queueFR.UsedFailedMomentMin = cbReceiptMoment;
            }

            if (_queueFR.UsedFailedMomentMax is null || cbReceiptMoment > _queueFR.UsedFailedMomentMax)
            {
                _queueFR.UsedFailedMomentMax = cbReceiptMoment;
            }
        }

        _queueFR.UsedFailedCount++;
        return opened;
    }

    /// <summary>Ends the mode and returns the range it covered.</summary>
    public FRLateSigningRange Close()
    {
        var range = new FRLateSigningRange(_queueFR.UsedFailedCount, _queueFR.UsedFailedQueueItemId, _queueFR.UsedFailedMomentMin, _queueFR.UsedFailedMomentMax);
        _queueFR.UsedFailedCount = 0;
        _queueFR.UsedFailedQueueItemId = null;
        _queueFR.UsedFailedMomentMin = null;
        _queueFR.UsedFailedMomentMax = null;
        return range;
    }
}

/// <summary>The late-signed receipts a closing zero receipt ends the mode for.</summary>
public record FRLateSigningRange(int Count, Guid? FirstQueueItemId, DateTime? MomentMin, DateTime? MomentMax);
