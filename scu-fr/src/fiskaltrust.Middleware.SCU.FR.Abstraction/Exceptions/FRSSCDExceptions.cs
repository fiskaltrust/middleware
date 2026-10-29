using System;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

/// <summary>Base type for all failures raised by French SCUs.</summary>
public class FRSSCDException : Exception
{
    public FRSSCDException(string message) : base(message) { }
    public FRSSCDException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// The signature could not be created — the private key is unavailable, the certificate expired,
/// or a remote signing endpoint is unreachable. An unsigned receipt breaks the NF525 chain, so
/// this failure carries its own ftState flag; see <c>StateFR.SigningUnavailableError</c>.
/// </summary>
public class FRSigningUnavailableException : FRSSCDException
{
    public FRSigningUnavailableException(string message) : base(message) { }
    public FRSigningUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>The configured signature creation data is unusable (bad key material, wrong curve, …).</summary>
public class FRCertificateException : FRSSCDException
{
    public FRCertificateException(string message) : base(message) { }
    public FRCertificateException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>A request violates an FR constraint before any signing happens.</summary>
public class FRValidationException : FRSSCDException
{
    public FRValidationException(string message) : base(message) { }
}
