using System;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// The sale failed, and the SCU has made sure the register did not issue the receipt: the failure
/// came before <c>trend</c> (a receipt only becomes fiscal when it is closed, and a closed receipt
/// is never sent after a failure), or the register's counters, read once it answered again, show
/// that <c>trend</c> did not complete one. Unlike an unknown outcome this is a definite answer,
/// so the caller may resend without risking a duplicate.
/// </summary>
public class PosNetReceiptNotIssuedException : PLSSCDException
{
    public PosNetReceiptNotIssuedException(string reason, Exception? innerException = null)
        : base($"The receipt was NOT issued — the fiscal register did not fiscalize it, so it can be resent once the cause is fixed. Reason: {reason}", innerException!)
    {
    }
}
