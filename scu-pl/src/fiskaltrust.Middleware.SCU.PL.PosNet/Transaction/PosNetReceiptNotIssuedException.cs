using System;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// The sale (or daily report) failed, and the SCU has made sure the register did not issue it: the failure
/// came before <c>trend</c> (a receipt only becomes fiscal when it is closed, and a closed receipt
/// is never sent after a failure), or the register's counters, read once it answered again, show
/// that <c>trend</c> did not complete one. Unlike an unknown outcome this is a definite answer,
/// so the caller may resend without risking a duplicate.
/// </summary>
public class PosNetReceiptNotIssuedException : PLSSCDException
{
    public PosNetReceiptNotIssuedException(string reason, Exception? innerException = null, string document = "receipt")
        : base($"The {document} was NOT issued — the fiscal register did not execute it, so it can be sent again once the cause is fixed. Reason: {reason}", innerException!)
    {
    }
}
