using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// The general additional-lines contract on a POSNET register: <c>ftReceiptCaseData.cbReceiptLines</c>
/// (lines for the whole receipt) and the payment slip of each pay item in <c>ftPayItemCaseData</c> —
/// <c>Receipt</c> (from the POS System API pay endpoint) or <c>cbPayItemLines</c> (filled by the POS),
/// <c>Receipt</c> winning when both are present.
/// </summary>
/// <remarks>
/// They are printed inside the fiscal receipt as left-aligned additional lines (<c>trftrln id41</c>),
/// after the <c>printout</c> lines: the receipt lines first, then the slips in pay item order. A slip
/// printed as a document of its own (super-form 200) took a header number of its own — the "nr" top
/// right — and was cut off from the receipt; inside the receipt it is one document, one number, one
/// cut (measured on a POSNET Thermal: <c>scnt hn</c> +1 for receipt and slip together). An entry with
/// line breaks is split into several lines, and an empty one prints as an empty line, as on
/// fiskaltrust's own rendering.
/// </remarks>
public static class PosNetAdditionalLines
{
    /// <summary>
    /// Printable characters per additional line on 80 mm paper (trftrln na, POT-I-DEV-37 v6403 p. 371);
    /// a register set to 40 characters per line (57 mm) refuses longer ones.
    /// </summary>
    public const int MaxLineLength = 56;

    /// <summary>
    /// The receipt lines and payment slips of the request as additional lines of the receipt. Every line
    /// is checked before anything reaches the register, like the footer codes are.
    /// </summary>
    public static IReadOnlyList<PosNetFooterLine> Read(ReceiptRequest request)
    {
        var receiptLines = Lines(ReadArray(request.ftReceiptCaseData, "cbReceiptLines"), "cbReceiptLines");
        var slipLines = (request.cbPayItems ?? []).SelectMany(payItem =>
            Lines(ReadArray(payItem.ftPayItemCaseData, "Receipt") ?? ReadArray(payItem.ftPayItemCaseData, "cbPayItemLines"),
                $"the payment slip of '{payItem.Description}'"));
        return receiptLines.Concat(slipLines)
            .Select(line => new PosNetFooterLine(ToLeftAlignedText(line), false, false, PosNetFooterLine.LeftAlignedLineId))
            .ToList();
    }

    private static IEnumerable<string> Lines(List<string>? entries, string source)
    {
        var lines = (entries ?? []).SelectMany(entry => entry.Replace("\r\n", "\n").Split('\n')).ToList();
        foreach (var line in lines.Where(l => l.Length > MaxLineLength))
        {
            throw new PLValidationException($"The line '{line}' in {source} has {line.Length} characters — a POSNET register prints at most {MaxLineLength} per line (80 mm paper; 40 on 57 mm).");
        }
        return lines;
    }

    private static List<string>? ReadArray(object? caseData, string name)
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
    /// A line as <c>trftrln id41</c> takes it: '&amp;' introduces a formatting code there (as in
    /// ftrinfoset), so a literal one is doubled; an empty line becomes a single space, as the printer
    /// needs text to print the line.
    /// </summary>
    public static string ToLeftAlignedText(string line) => line.Length == 0 ? " " : line.Replace("&", "&&");
}
