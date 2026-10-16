using System;
using System.Globalization;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.PL.PosNet.Client;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// What the register contributed to a receipt it printed, read back right after <c>trend</c>: the
/// receipt number alone is no key — a register with a daily receipt counter repeats it every day —
/// so the receipt is anchored by the tuple numer unikatowy + daily report + receipt number + device
/// moment, with the protected-memory document number as the link into the register's archive.
/// </summary>
/// <param name="ReceiptNumber">The receipt number printed on the document (<c>scnt.bt</c>).</param>
/// <param name="DailyReportNumber">
/// The daily report the receipt is booked into: the one still to be printed, <c>scnt.rd + 1</c>.
/// </param>
/// <param name="DocumentNumber">The protected-memory document number (<c>eclastdocnoget ty0</c> → <c>di</c>).</param>
/// <param name="DeviceMoment">
/// The register's clock right after the receipt (<c>rtcget</c>), ISO 8601 with offset — to the second
/// where the register answers <c>tm</c>, to the minute where it only answers <c>da</c>.
/// </param>
public sealed record PosNetReceiptAnchor(long ReceiptNumber, long? DailyReportNumber, long? DocumentNumber, string? DeviceMoment);

/// <summary>The receipt counters before <c>trinit</c>: the baseline the readback is checked against.</summary>
/// <param name="CompletedReceipts">Correctly completed receipts (<c>scnt.bn</c>).</param>
/// <param name="LastReceiptNumber">The last receipt number (<c>scnt.bt</c>), which canceled receipts advance as well.</param>
public sealed record PosNetReceiptCounters(long? CompletedReceipts, long? LastReceiptNumber);

/// <summary>
/// Reads back the anchor of a receipt (<see cref="PosNetReceiptAnchor"/>). The protocol only pulls:
/// <c>trend</c> answers with a plain confirmation, and every status command describes the
/// <em>last</em> document rather than this one — so a receipt printed in between over another
/// interface would silently lend this one its numbers. The receipt counters guard against that:
/// they are read before <c>trinit</c> and again as the last readback, and only a register that
/// completed exactly one receipt (<c>bn</c>) and numbered exactly one (<c>bt</c>, which a receipt
/// canceled elsewhere advances too) proves that every number read in between belongs to this receipt.
/// </summary>
/// <remarks>
/// The receipt is printed by the time the readback runs, so no readback may fail it: a read that
/// fails leaves its value out, and a guard that fails or cannot be checked leaves the whole anchor
/// out — a missing anchor can be reconciled from the register, a wrong one would silently point at
/// another customer's document. All reads are status commands and cannot change the register.
/// </remarks>
public static class PosNetReceiptReadback
{
    private const string IsoMomentFormat = "yyyy-MM-dd'T'HH:mm:sszzz";
    private const string MinuteMomentFormat = "yyyy-MM-dd'T'HH:mmzzz";

    /// <summary>
    /// The receipt counters before the transaction is opened. Nothing has been sent that could
    /// print yet, so a failing read propagates like any other failure before <c>trinit</c>; a
    /// register that does not report a counter yields null for it, and its receipts then go
    /// unanchored rather than unchecked.
    /// </summary>
    public static async Task<PosNetReceiptCounters> ReadCountersAsync(PosNetClient client)
    {
        var counters = await client.ExecuteAsync(PosNetCommands.Scnt());
        return new PosNetReceiptCounters(PosNetStatus.ReadNumber(counters, "bn"), PosNetStatus.ReadNumber(counters, "bt"));
    }

    /// <summary>
    /// Reads the anchor of the receipt just printed, or null when it cannot be told apart from
    /// another document. <c>scnt</c> goes last because it carries the guard: the reads in front of
    /// it are only trusted once the counters show that no other receipt was completed or canceled meanwhile.
    /// </summary>
    public static async Task<PosNetReceiptAnchor?> ReadAsync(PosNetClient client, PosNetReceiptCounters before, TimeZoneInfo? registerZone)
    {
        var documentNumber = await PosNetStatus.TryReadAsync(client, PosNetCommands.Eclastdocnoget(PosNetCommands.ReceiptDocumentType), ReadDocumentNumber);
        var deviceMoment = await PosNetStatus.TryReadAsync(client, PosNetCommands.Rtcget(), status => ReadDeviceMoment(status, registerZone));
        var counters = await PosNetStatus.TryReadAsync(client, PosNetCommands.Scnt(), status => status);

        if (counters is null
            || before.CompletedReceipts is not { } completedBefore
            || before.LastReceiptNumber is not { } numberBefore
            || PosNetStatus.ReadNumber(counters, "bn") != completedBefore + 1
            || PosNetStatus.ReadNumber(counters, "bt") is not { } receiptNumber
            || receiptNumber != numberBefore + 1)
        {
            return null;
        }

        var dailyReportNumber = PosNetStatus.ReadNumber(counters, "rd") is { } reportsDone ? reportsDone + 1 : (long?)null;
        return new PosNetReceiptAnchor(receiptNumber, dailyReportNumber, documentNumber, deviceMoment);
    }

    /// <summary>
    /// The register's clock as ISO 8601 with offset, or null when it cannot be told without guessing.
    /// <c>tm</c> is only taken when it states its offset itself — parsed without one, it would take the
    /// host's zone. <c>da</c>, all that older firmware answers, is the register's wall clock on Polish
    /// time and is kept to the minute it carries. In the hour the clocks go back that wall clock occurs
    /// twice and is read as the later, standard-time one: <c>da</c> cannot say which.
    /// </summary>
    public static string? ReadDeviceMoment(PosNetResponse status, TimeZoneInfo? registerZone)
    {
        if (status.Parameters.TryGetValue("tm", out var isoMoment)
            && HasOffset(isoMoment)
            && DateTimeOffset.TryParse(isoMoment, CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment))
        {
            return moment.ToString(IsoMomentFormat, CultureInfo.InvariantCulture);
        }
        if (registerZone is not null
            && status.Parameters.TryGetValue("da", out var wallClock)
            && DateTime.TryParseExact(wallClock, ["yyyy-MM-dd;HH:mm", "yyyy-MM-dd;H:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return new DateTimeOffset(local, registerZone.GetUtcOffset(local)).ToString(MinuteMomentFormat, CultureInfo.InvariantCulture);
        }
        return null;
    }

    /// <summary>Whether an ISO 8601 moment ends in an offset (<c>+02:00</c>, <c>-0100</c>) or <c>Z</c>.</summary>
    private static bool HasOffset(string isoMoment)
    {
        var timeStart = isoMoment.IndexOf('T');
        if (timeStart < 0)
        {
            return false;
        }
        var time = isoMoment.AsSpan(timeStart);
        return time.EndsWith("Z", StringComparison.OrdinalIgnoreCase) || time.IndexOfAny('+', '-') >= 0;
    }

    /// <summary><c>di0</c> is the register's answer for "no such document" — not a document number.</summary>
    private static long? ReadDocumentNumber(PosNetResponse status)
        => PosNetStatus.ReadNumber(status, "di") is > 0 and var number ? number : null;
}
