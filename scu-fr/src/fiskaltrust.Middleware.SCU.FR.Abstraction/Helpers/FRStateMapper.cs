using System;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

public static class FRStateMapper
{
    /// <summary>
    /// Maps an SCU failure to the FR-localized ftState. Signing failures get their own local flag
    /// so queues and monitoring can tell a broken signature chain from an ordinary error.
    /// </summary>
    public static State Map(Exception exception) => exception switch
    {
        FRSigningUnavailableException => (State) StateFR.SigningUnavailableError,
        FRCertificateException => (State) StateFR.SigningUnavailableError,
        _ => (State) StateFR.Error,
    };
}
