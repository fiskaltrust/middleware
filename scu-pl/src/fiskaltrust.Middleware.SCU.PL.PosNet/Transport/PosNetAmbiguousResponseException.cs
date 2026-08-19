using System;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transport;

/// <summary>
/// The command frame was written to the device, but no (complete) response arrived: the printer
/// may or may not have executed the command and printed a fiscal document. Such an outcome must
/// never be silently retried — a blind retry can duplicate fiscal printouts. It derives from
/// <see cref="PLDeviceUnreachableException"/> because the legal consequence is the same as an
/// unreachable register (Art. 111(3): no confirmed fiscalization — no sale): the queue responds
/// with DeviceUnreachableError and the operator verifies the device state before resending.
/// </summary>
public class PosNetAmbiguousResponseException : PLDeviceUnreachableException
{
    public PosNetAmbiguousResponseException(string message) : base(message) { }
    public PosNetAmbiguousResponseException(string message, Exception innerException) : base(message, innerException) { }
}
