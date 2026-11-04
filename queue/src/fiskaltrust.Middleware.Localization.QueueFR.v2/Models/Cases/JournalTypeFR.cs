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
/// Training 005 to 009 instead; see fiskaltrust/market-fr#173. The chain letters in the summaries
/// are the contract's; v2 <c>FRReceiptChain</c> still uses "D" for copies and has no "A" chain
/// (fiskaltrust/market-fr#173, #114). Only the <c>tjjj</c> part identifies the journal: the
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

    /// <summary>Grand total ("G" group) export.</summary>
    GrandTotal = 0x4652_2000_0000_0004,

    /// <summary>Bill ("B" group) export.</summary>
    Bill = 0x4652_2000_0000_0007,

    /// <summary>Archive ("A" group) export: the archive receipts themselves.</summary>
    Archive = 0x4652_2000_0000_0008,

    /// <summary>Log ("L" group) export.</summary>
    Log = 0x4652_2000_0000_0009,

    /// <summary>Copy ("C" group) export: duplicates of issued documents.</summary>
    Copy = 0x4652_2000_0000_000A,

    /// <summary>Training ("X" group) export.</summary>
    Training = 0x4652_2000_0000_000B,

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
