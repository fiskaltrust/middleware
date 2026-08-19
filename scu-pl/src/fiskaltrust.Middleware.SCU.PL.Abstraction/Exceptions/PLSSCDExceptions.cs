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
/// so this failure carries its own ftState flag — see <c>StatePL.DeviceUnreachableError</c>.
/// </summary>
public class PLDeviceUnreachableException : PLSSCDException
{
    public PLDeviceUnreachableException(string message) : base(message) { }
    public PLDeviceUnreachableException(string message, Exception innerException) : base(message, innerException) { }
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
}
