using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

/// <summary>Shared ftState flags the interface does not name yet.</summary>
public static class StateFlagsFR
{
    /// <summary>
    /// An archive receipt is due: the open archive file reaches into an earlier calendar month (UTC)
    /// than the receipt (market-fr ADR 0018).
    /// </summary>
    // TODO: use StateFlags.ArchiveIsDue once the interface that adds it (fiskaltrust.interface 1.3.80-rc1) is referenced.
    public const StateFlags ArchiveIsDue = (StateFlags) 0x0800;
}
