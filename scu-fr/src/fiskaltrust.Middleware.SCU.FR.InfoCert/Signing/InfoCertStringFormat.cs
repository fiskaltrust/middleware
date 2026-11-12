using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

/// <summary>
/// The value formats of the strings Infocert's dictionaries sign (R13 v4.2 and R12 v4.2 §4, R19 v4.2): amounts in
/// euro cents, timestamps <c>AAAAMMJJHHMMSS</c>, the VAT breakdown as <c>rate:amount</c> pairs joined with
/// <c>|</c>, fields joined with <c>,</c>, and text with the separators replaced by documented substitutes.
/// </summary>
/// <remarks>
/// Two choices were product decisions (market-fr#221, 2026-11-12); each is kept in one place:
/// <list type="bullet">
/// <item>the time zone of the signed timestamps: <see cref="SignedTimeZone"/> (UTC);</item>
/// <item>which charge items make the document amount: <see cref="CountsInDocumentAmount"/> (every item, down payments included).</item>
/// </list>
/// </remarks>
internal static class InfoCertStringFormat
{
    public const char FieldSeparator = ',';
    public const char RateSeparator = '|';
    public const char RateAmountSeparator = ':';

    /// <summary>R13/R12 §4 let the editor choose the substitute for a separator inside a text field.</summary>
    public const char FieldSeparatorSubstitute = ';';
    public const char RateSeparatorSubstitute = '/';

    /// <summary>
    /// The zone the signed timestamps are written in.
    /// UTC (decided 2026-11-12, market-fr#221): the archive stores every moment in UTC, so a verifier rebuilds the string without time-zone rules.
    /// </summary>
    public static readonly TimeZoneInfo SignedTimeZone = TimeZoneInfo.Utc;

    /// <summary>
    /// Whether a charge item counts in the
    /// signed amounts of a ticket, note or invoice. Every item counts (decided 2026-11-12, market-fr#221), discounts, refunds and down payments included:
    /// the document amount is what is printed and what the receipt VAT summary sums.
    /// </summary>
    public static bool CountsInDocumentAmount(ChargeItem item) => true;

    /// <summary>Euro cents, no decimal point, a leading <c>-</c> when negative: <c>68.55</c> → <c>6855</c>.</summary>
    public static string Money(decimal amount)
        => ((long) Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);

    /// <summary><c>AAAAMMJJHHMMSS</c> in <see cref="SignedTimeZone"/>, below the second dropped.</summary>
    public static string Timestamp(DateTime moment)
        => TimeZoneInfo.ConvertTimeFromUtc(moment.ToUniversalTime(), SignedTimeZone).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

    /// <summary>The rate on four digits, the rate times 100: 20 → <c>2000</c>, 5.5 → <c>0550</c>, 2.1 → <c>0210</c>, 0 → <c>0000</c>.</summary>
    /// A rate is written to the hundredth (rounded half away from zero), as the printed VAT summary groups it: two rates
    /// closer than that make one line.
    /// <exception cref="FRValidationException">The rate is negative or 100 % or more.</exception>
    public static string Rate(decimal rate)
    {
        var hundredths = Math.Round(rate * 100m, 0, MidpointRounding.AwayFromZero);
        if (hundredths < 0 || hundredths > 9999)
        {
            throw new FRValidationException($"The VAT rate {rate.ToString(CultureInfo.InvariantCulture)} cannot be signed on four digits (R13 v4.2 §4).");
        }
        return ((int) hundredths).ToString("0000", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The VAT breakdown of a document: for each rate, highest first, <c>rate:amount</c> (tax included), joined with
    /// <c>|</c>. Empty when the document has no charge item counting in its amount.
    /// </summary>
    public static string Breakdown(IEnumerable<ChargeItem>? chargeItems)
        => Breakdown((chargeItems ?? Enumerable.Empty<ChargeItem>())
            .Where(CountsInDocumentAmount)
            .GroupBy(x => Rate(x.VATRate))
            .OrderByDescending(x => x.Key, StringComparer.Ordinal)
            .Select(x => (x.Key, x.Sum(item => item.Amount))));

    /// <summary>A breakdown from already keyed amounts, in the order given.</summary>
    public static string Breakdown(IEnumerable<(string Key, decimal Amount)> amounts)
        => string.Join(RateSeparator.ToString(), amounts.Select(x => $"{x.Key}{RateAmountSeparator}{Money(x.Amount)}"));

    /// <summary>The document amount: the sum of the charge items that count (<see cref="CountsInDocumentAmount"/>).</summary>
    public static decimal DocumentAmount(IEnumerable<ChargeItem>? chargeItems)
        => (chargeItems ?? Enumerable.Empty<ChargeItem>()).Where(CountsInDocumentAmount).Sum(x => x.Amount);

    /// <summary>Text as sent, with <c>,</c> → <c>;</c>, <c>|</c> → <c>/</c> and line breaks → space, so every field splits exactly.</summary>
    public static string Text(string? value)
        => (value ?? "")
            .Replace(FieldSeparator, FieldSeparatorSubstitute)
            .Replace(RateSeparator, RateSeparatorSubstitute)
            .Replace('\r', ' ')
            .Replace('\n', ' ');

    /// <summary>The fields of one signed string, joined with <c>,</c>. Callers pass values already formatted.</summary>
    public static string Join(IEnumerable<string> fields) => string.Join(FieldSeparator.ToString(), fields);
}
