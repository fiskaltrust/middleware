using System;
using System.Globalization;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
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
/// <param name="DailyReportNumber">The daily report the receipt is booked into: the next one, <c>scnt.rd + 1</c>.</param>
/// <param name="DocumentNumber">The protected-memory document number (<c>eclastdocnoget ty0</c> → <c>di</c>).</param>
/// <param name="DeviceMoment">The register's clock right after the receipt (<c>rtcget</c>).</param>
public sealed record PosNetReceiptAnchor(long ReceiptNumber, long? DailyReportNumber, long? DocumentNumber, DateTimeOffset? DeviceMoment);

/// <summary>
/// Reads back the anchor of a receipt (<see cref="PosNetReceiptAnchor"/>). The protocol only pulls:
/// <c>trend</c> answers with a plain confirmation, and every status command describes the
/// <em>last</em> document rather than this one — so a receipt printed in between over another
/// interface would silently lend this one its numbers. The completed-receipt counter guards against
/// that: it is read before <c>trinit</c> and again as the last readback, and only a counter that
/// advanced by exactly one proves that every number read in between belongs to this receipt.
/// </summary>
/// <remarks>
/// The receipt is printed by the time the readback runs, so no readback may fail it: a read that
/// fails leaves its value out, and a guard that fails or cannot be checked leaves the whole anchor
/// out — a missing anchor can be reconciled from the register, a wrong one would silently point at
/// another customer's document. All reads are status commands and cannot change the register.
/// </remarks>
public static class PosNetReceiptReadback
{
    /// <summary>
    /// The completed-receipt counter (<c>scnt.bn</c>) before the transaction is opened — the
    /// baseline of the guard. Nothing has been sent that could print yet, so a failing read
    /// propagates like any other failure before <c>trinit</c>; a register that does not report the
    /// counter yields null, and its receipts then go unanchored rather than unchecked.
    /// </summary>
    public static async Task<long?> ReadCompletedReceiptsAsync(PosNetClient client)
        => ReadNumber(await client.ExecuteAsync(PosNetCommands.Scnt()), "bn");

    /// <summary>
    /// Reads the anchor of the receipt just printed, or null when it cannot be told apart from
    /// another document. <c>scnt</c> goes last because it carries the guard: the reads in front of
    /// it are only trusted once the counter shows that no other receipt was completed meanwhile.
    /// </summary>
    public static async Task<PosNetReceiptAnchor?> ReadAsync(PosNetClient client, long? completedReceiptsBefore, string registerTimeZoneId)
    {
        var documentNumber = await TryReadAsync(client, PosNetCommands.Eclastdocnoget(PosNetCommands.ReceiptDocumentType), ReadDocumentNumber);
        var deviceMoment = await TryReadAsync(client, PosNetCommands.Rtcget(), status => ReadDeviceMoment(status, registerTimeZoneId));
        var counters = await TryReadAsync(client, PosNetCommands.Scnt(), status => status);

        if (counters is null
            || completedReceiptsBefore is null
            || ReadNumber(counters, "bn") != completedReceiptsBefore + 1
            || ReadNumber(counters, "bt") is not { } receiptNumber)
        {
            return null;
        }

        var dailyReportNumber = ReadNumber(counters, "rd") is { } reportsDone ? reportsDone + 1 : (long?)null;
        return new PosNetReceiptAnchor(receiptNumber, dailyReportNumber, documentNumber, deviceMoment);
    }

    /// <summary>
    /// The register's clock as ISO 8601 with offset. <c>tm</c> carries the offset itself; <c>da</c>,
    /// all that older firmware answers, is the register's wall clock, which runs on Polish time.
    /// </summary>
    public static DateTimeOffset? ReadDeviceMoment(PosNetResponse status, string registerTimeZoneId)
    {
        if (status.Parameters.TryGetValue("tm", out var isoMoment)
            && DateTimeOffset.TryParse(isoMoment, CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment))
        {
            return moment;
        }
        if (status.Parameters.TryGetValue("da", out var wallClock)
            && DateTime.TryParseExact(wallClock, ["yyyy-MM-dd;HH:mm", "yyyy-MM-dd;H:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(registerTimeZoneId);
            return new DateTimeOffset(local, zone.GetUtcOffset(local));
        }
        return null;
    }

    /// <summary><c>di0</c> is the register's answer for "no such document" — not a document number.</summary>
    private static long? ReadDocumentNumber(PosNetResponse status)
        => ReadNumber(status, "di") is > 0 and var number ? number : null;

    private static long? ReadNumber(PosNetResponse status, string field)
        => status.Parameters.TryGetValue(field, out var text)
            && long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                ? number
                : null;

    private static async Task<T?> TryReadAsync<T>(PosNetClient client, PosNetCommand command, Func<PosNetResponse, T?> read)
    {
        try
        {
            return read(await client.ExecuteAsync(command));
        }
        catch (PLSSCDException)
        {
            // A status read cannot change the register, so a failed or ambiguous one is safe to
            // drop — the value is simply not part of the anchor.
            return default;
        }
    }
}
