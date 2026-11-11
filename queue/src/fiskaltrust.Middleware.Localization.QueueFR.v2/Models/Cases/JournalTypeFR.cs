using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

/// <summary>
/// FR-localized journal types (<c>ftJournalType</c>, format <c>CCCC_vlll_gggg_tjjj</c>): the queue
/// status and one export per receipt group, plus the archive export.
/// </summary>
/// <remarks>
/// The <c>jjj</c> codes follow the published journal-type table and the v2 archive contract
/// (fiskaltrust/market-fr <c>doc/nf525/contracts/archive-v2.md</c>, section 7): codes 005 and 006
/// are unassigned. The v1.3 QueueFR (<c>Contracts.Constants.JournalTypes</c>) numbers Bill to
/// Training 005 to 009 instead; see fiskaltrust/market-fr#173. Since 2026-11-04 the journals follow the
/// v2 chains of ADR 0002: 004 is the technical chain E, 00A the copies D, 00C to 00F the closing chains
/// S, Z, M, Y. Since 2026-11-09, 008 is again the archive receipts, now the v2 archive chain A
/// (market-fr#221). Only the <c>tjjj</c> part identifies the journal: the
/// country, the version nibble and the <c>gggg</c> flags are ignored when matching.
/// </remarks>
public enum JournalTypeFR : long
{
    /// <summary>Status information of the QueueFR.</summary>
    StatusInformation = 0x4652_2000_0000_0000,

    /// <summary>Ticket ("T" group) export.</summary>
    Ticket = 0x4652_2000_0000_0001,

    /// <summary>Payment proof ("P" group) export.</summary>
    PaymentProof = 0x4652_2000_0000_0002,

    /// <summary>Invoice ("I" group) export.</summary>
    Invoice = 0x4652_2000_0000_0003,

    /// <summary>Technical chain ("E") export: zero receipts, lifecycle and the other non-closing daily operations.</summary>
    Technical = 0x4652_2000_0000_0004,

    /// <summary>Bill ("B" group) export.</summary>
    Bill = 0x4652_2000_0000_0007,

    /// <summary>Archive chain ("A") export: the archive receipts.</summary>
    Archive = 0x4652_2000_0000_0008,

    /// <summary>Log ("L" group) export.</summary>
    Log = 0x4652_2000_0000_0009,

    /// <summary>Copy chain ("D") export: duplicates of issued documents.</summary>
    Copy = 0x4652_2000_0000_000A,

    /// <summary>Training ("X" group) export.</summary>
    Training = 0x4652_2000_0000_000B,

    /// <summary>Shift closing chain ("S") export.</summary>
    ShiftClosing = 0x4652_2000_0000_000C,

    /// <summary>Daily closing chain ("Z") export.</summary>
    DailyClosing = 0x4652_2000_0000_000D,

    /// <summary>Monthly closing chain ("M") export.</summary>
    MonthlyClosing = 0x4652_2000_0000_000E,

    /// <summary>Yearly closing chain ("Y") export.</summary>
    YearlyClosing = 0x4652_2000_0000_000F,

    /// <summary>Export of the fiscal archive produced by an archive receipt.</summary>
    ArchiveExport = 0x4652_2000_0000_0010,
}

public static class JournalTypeFRExt
{
    public static T As<T>(this JournalTypeFR self) where T : Enum, IConvertible => (T) Enum.ToObject(typeof(T), self);

    /// <summary>The journal code (<c>tjjj</c>) of a request's journal type, as a <see cref="JournalTypeFR"/>.</summary>
    public static JournalTypeFR TypeFR(this JournalType self) => (JournalTypeFR) (0x4652_2000_0000_0000 | ((long) self & 0xFFFF));

    public static bool IsType(this JournalType self, JournalTypeFR journalTypeFR) => ((long) self & 0xFFFF) == ((long) journalTypeFR & 0xFFFF);

    public static bool IsKnown(this JournalTypeFR self) => Enum.IsDefined(typeof(JournalTypeFR), self);
}
