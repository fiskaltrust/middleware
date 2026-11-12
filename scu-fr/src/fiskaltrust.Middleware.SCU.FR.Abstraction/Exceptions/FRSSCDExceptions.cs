using System;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

/// <summary>
/// Base type for the failures French SCUs raise while starting up. Once running, an SCU reports a
/// failure in its response instead (see <c>FRReceiptResponseExtensions.ToSigningFailure</c>).
/// </summary>
public class FRSSCDException : Exception
{
    public FRSSCDException(string message) : base(message) { }
    public FRSSCDException(string message, Exception innerException) : base(message, innerException) { }
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
