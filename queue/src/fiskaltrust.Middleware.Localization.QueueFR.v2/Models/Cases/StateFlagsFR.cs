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

    /// <summary>A daily closing is due: the open day began more than 24 hours before the receipt (contract §1.4).</summary>
    // TODO: use StateFlags.DailyClosingIsDue once fiskaltrust.interface 1.3.80 is referenced.
    public const StateFlags DailyClosingIsDue = (StateFlags) 0x0100;

    /// <summary>A monthly closing is due: the open month began in an earlier calendar month (UTC) than the receipt.</summary>
    // TODO: use StateFlags.MonthlyClosingIsDue once fiskaltrust.interface 1.3.80 is referenced.
    public const StateFlags MonthlyClosingIsDue = (StateFlags) 0x0200;

    /// <summary>A yearly closing is due: the open year began in an earlier calendar year (UTC) than the receipt.</summary>
    // TODO: use StateFlags.YearlyClosingIsDue once fiskaltrust.interface 1.3.80 is referenced.
    public const StateFlags YearlyClosingIsDue = (StateFlags) 0x0400;
}
