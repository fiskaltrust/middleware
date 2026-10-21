using System;

namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

/// <summary>Base type for all failures raised by Polish SCUs.</summary>
public class PLSSCDException : Exception
{
    public PLSSCDException(string message) : base(message) { }
    public PLSSCDException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// The fiscal register could not be reached (offline printer, network failure, printer farm
/// endpoint down). In Poland no working register legally means no sale (Art. 111(3) VAT Act),
/// so the queue tells the caller to retry once the register is available.
/// </summary>
public class PLDeviceUnreachableException : PLSSCDException
{
    public PLDeviceUnreachableException(string message) : base(message) { }
    public PLDeviceUnreachableException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// A command reached the register, but whether it was executed is unknown (no answer, or one that
/// cannot be attributed or read) — a fiscal document may have been printed. Legally it is the same
/// as an unreachable register (no confirmed fiscalization — no sale), hence the base type; unlike
/// an unreachable register it must not simply be retried, because a resend can print the document
/// twice — the queue's error message tells the operator to check the register before resending.
/// </summary>
public class PLDeviceOutcomeUnknownException : PLDeviceUnreachableException
{
    public PLDeviceOutcomeUnknownException(string message) : base(message) { }
    public PLDeviceOutcomeUnknownException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>The register rejected an operation with a device error code.</summary>
public class PLDeviceErrorException : PLSSCDException
{
    public int ErrorCode { get; }

    public PLDeviceErrorException(int errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>A request violates a PL constraint before any device communication happens.</summary>
public class PLValidationException : PLSSCDException
{
    public PLValidationException(string message) : base(message) { }
    public PLValidationException(string message, Exception innerException) : base(message, innerException) { }
}
