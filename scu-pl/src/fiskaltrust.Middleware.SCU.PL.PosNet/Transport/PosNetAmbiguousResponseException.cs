using System;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// The command frame was written to the device, but no (complete) response arrived: the printer
/// may or may not have executed the command and printed a fiscal document. Such an outcome must
/// never be silently retried — a blind retry can duplicate fiscal printouts. It derives from
/// <see cref="PLDeviceOutcomeUnknownException"/>: the legal consequence is the same as an
/// unreachable register (Art. 111(3): no confirmed fiscalization — no sale), so the queue responds
/// with DeviceUnreachableError, but its message asks the operator to verify the device state before resending.
/// </summary>
public class PosNetAmbiguousResponseException : PLDeviceOutcomeUnknownException
{
    public PosNetAmbiguousResponseException(string message) : base(message) { }
    public PosNetAmbiguousResponseException(string message, Exception innerException) : base(message, innerException) { }
}
