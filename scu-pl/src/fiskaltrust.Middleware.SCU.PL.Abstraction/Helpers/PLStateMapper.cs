using System;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;

public static class PLStateMapper
{
    /// <summary>
    /// Maps an SCU failure to the PL-localized ftState. Device-unreachable failures get their own
    /// local flag so queues and monitoring can distinguish "no register — no legal sale" from
    /// ordinary errors.
    /// </summary>
    public static State Map(Exception exception) => exception switch
    {
        PLDeviceUnreachableException => (State)StatePL.DeviceUnreachableError,
        _ => (State)StatePL.Error,
    };
}
