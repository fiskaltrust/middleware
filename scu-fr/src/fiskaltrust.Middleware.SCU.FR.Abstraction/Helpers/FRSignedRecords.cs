using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// The structured fields of the 25-field data set (archive-v2 §5): field 20, and the grammar it shares with field 21.
/// A field is a list of records separated by <c>;</c>, each a list of values separated by <c>,</c>. No value may
/// contain <c>|</c> (the data-set field separator), <c>;</c> or <c>,</c>. Moments are written as field 12
/// (<c>ftReceiptMoment</c>): round-trip ("O") UTC.
/// </summary>
/// <remarks>
/// Kept in the abstraction so every SCU that signs the 25-field data set encodes these fields once (market-fr#221).
/// </remarks>
public static class FRSignedRecords
{
    public const char RecordSeparator = ';';
    public const char ValueSeparator = ',';

    private const long ArchiveReceipt = 0x3011;
    private const long FinishMigration = 0x4022;
    private const long OutOfOperation = 0x4002;
    private const long ShiftClosing = 0x2010;
    private const long YearlyClosing = 0x2013;

    /// <summary>
    /// Field 20, chosen by the receipt case (field 13); empty on every other receipt and on a training receipt:
    /// <list type="bullet">
    /// <item>0x3011 (Archive receipt): one record, the archive boundaries (<see cref="FRArchiveBoundaries.SignedField"/>);</item>
    /// <item>0x4022 (Finish migration): one record per v1 chain, <c>letter,counter,lastHash,lastSignature</c> (market-fr ADR 0003);</item>
    /// <item>0x2010–0x2013 (Shift / Daily / Monthly / Yearly closing receipt) and 0x4002 (Queue-stop receipt): one record
    /// per period the receipt closes, shortest first, <c>period,start</c>. Each period ends at field 12.</item>
    /// </list>
    /// 0x4001 (Queue-start receipt) signs no period: a v2 queue accepts it only while new, so there are no totals to close.
    /// </summary>
    /// <exception cref="FRValidationException">A value contains a separator.</exception>
    public static string Field20(ReceiptRequest request, ReceiptResponse response)
    {
        if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training))
        {
            return "";
        }

        var receiptCase = (long) request.ftReceiptCase & 0xFFFF;
        if (receiptCase == ArchiveReceipt)
        {
            return FRStateData.Archive(response) is { } archive ? Join(new[] { FRArchiveBoundaries.SignedField(archive).Split(ValueSeparator) }) : "";
        }
        if (receiptCase == FinishMigration)
        {
            return FRStateData.Migration(response) is { } migration ? MigrationRecords(migration) : "";
        }
        if (receiptCase is >= ShiftClosing and <= YearlyClosing || receiptCase == OutOfOperation)
        {
            return ClosedPeriodRecords(FRStateData.ClosedPeriods(response));
        }
        return "";
    }

    /// <summary>The migration event, one record per v1 chain in the queue's order.</summary>
    public static string MigrationRecords(FRMigrationEvent migration)
        => Join(migration.Chains.Select(chain => new[]
        {
            chain.Chain,
            chain.Counter.ToString(CultureInfo.InvariantCulture),
            chain.LastHash ?? "",
            chain.LastSignature ?? "",
        }));

    /// <summary>The closed periods, shortest first, as <c>period,start</c>.</summary>
    public static string ClosedPeriodRecords(IEnumerable<FRClosedPeriod> periods)
        => Join(periods.OrderBy(x => x.Period).Select(x => new[] { x.Period.ToString(), Moment(x.Start) }));

    /// <summary>Moments as field 12 writes them: round-trip ("O") UTC.</summary>
    public static string Moment(DateTime moment) => moment.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    /// <summary>Joins records into one field, refusing a value that holds a separator: it would shift every value after it.</summary>
    /// <exception cref="FRValidationException">A value contains <c>|</c>, <c>;</c> or <c>,</c>.</exception>
    public static string Join(IEnumerable<IEnumerable<string>> records)
        => string.Join(RecordSeparator, records.Select(record => string.Join(ValueSeparator, record.Select(Checked))));

    private static string Checked(string value)
    {
        if (value.IndexOfAny(new[] { '|', RecordSeparator, ValueSeparator }) >= 0)
        {
            throw new FRValidationException($"The value '{value}' cannot be signed: it contains '|', ';' or ','.");
        }
        return value;
    }
}
