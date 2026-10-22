using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// The payment slip of a pay item, as the general additional-lines contract carries it in
/// <c>ftPayItemCaseData</c>: <c>Receipt</c> (from the POS System API pay endpoint) or
/// <c>cbPayItemLines</c> (filled by the POS); <c>Receipt</c> wins when both are present. On a POSNET
/// register it is printed after the fiscal receipt as a non-fiscal super-form. Which header the
/// slip should carry is still open (middleware#844).
/// </summary>
public sealed record PosNetPaymentSlip(IReadOnlyList<string> Lines)
{
    /// <summary>The super-form (formatka 200), which takes free formatted lines.</summary>
    public const int SuperForm = 200;

    /// <summary>
    /// Header 53 of the super-form; a POSNET Thermal prints it as "Wymiana środków płatności".
    /// Whether another header ("Pokwitowanie") fits a card slip better is open (middleware#844).
    /// </summary>
    public const int CardPaymentHeader = 53;

    /// <summary>Printable characters per super-form line (formformattedline s1).</summary>
    public const int MaxLineLength = 56;

    public const int MaxLines = 60;

    /// <summary>
    /// The slips of the request's pay items, in pay item order. A slip that cannot be printed as sent
    /// is refused before anything reaches the register, like the footer codes are.
    /// </summary>
    public static IReadOnlyList<PosNetPaymentSlip> Read(ReceiptRequest request)
        => (request.cbPayItems ?? []).Select(ReadPayItem).OfType<PosNetPaymentSlip>().ToList();

    private static PosNetPaymentSlip? ReadPayItem(PayItem payItem)
    {
        var lines = ReadLines(payItem.ftPayItemCaseData, "Receipt") ?? ReadLines(payItem.ftPayItemCaseData, "cbPayItemLines");
        if (lines is null || lines.Count == 0)
        {
            return null;
        }
        if (lines.Count > MaxLines)
        {
            throw new PLValidationException($"The payment slip of '{payItem.Description}' has {lines.Count} lines — a POSNET register prints at most {MaxLines}.");
        }
        foreach (var line in lines.Where(l => l.Length > MaxLineLength))
        {
            throw new PLValidationException($"The payment slip line '{line}' has {line.Length} characters — a POSNET register prints at most {MaxLineLength} (formformattedline s1).");
        }
        return new PosNetPaymentSlip(lines);
    }

    private static List<string>? ReadLines(object? caseData, string name)
    {
        var json = caseData switch
        {
            null => null,
            string text => string.IsNullOrWhiteSpace(text) ? null : text,
            JsonElement element => element.ValueKind == JsonValueKind.Null ? null : element.GetRawText(),
            _ => JsonSerializer.Serialize(caseData),
        };
        if (json is null)
        {
            return null;
        }
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !PLReceiptCaseDataReader.TryGetPropertyIgnoreCase(document.RootElement, name, out var value)
                || value.ValueKind != JsonValueKind.Array)
            {
                return null;
            }
            return value.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? "" : e.ToString()).ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// A slip line as the super-form takes it: '&amp;' introduces a formatting code there, so a literal
    /// one is doubled; an empty line becomes a single space, as the line parameter is required.
    /// </summary>
    public static string ToFormLine(string line) => line.Length == 0 ? " " : line.Replace("&", "&&");
}
