using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.Localization.QueueIT.Models;

/// <summary>
/// The RT identification of a document as reported by the SCU, the part of it that goes into the <c>ftJournalIT</c> table.
/// </summary>
public struct ScuResponse
{
    public ReceiptCase ftReceiptCase { get; set; }
    public DateTime ReceiptDateTime { get; set; }
    public long ReceiptNumber { get; set; }
    public long ZRepNumber { get; set; }
    public string? DataJson { get; set; }
}
