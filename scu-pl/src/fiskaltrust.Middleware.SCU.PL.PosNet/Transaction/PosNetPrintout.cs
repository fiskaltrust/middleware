using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// What is printed on the fiscal receipt beyond the fiscal content. Most of it a POS asks for in
/// <c>ftReceiptCaseData</c> as <c>{ "PL": { "printout": { … } } }</c> (next to <c>PL.eReceipt</c>, see
/// <see cref="PLReceiptCaseDataReader"/>):
/// <code>
/// { "PL": { "printout": {
///     "barcode": "1234567890",                                   // 1D code in the receipt footer
///     "qrCode":  { "data": "https://…", "pixelSize": 2, "errorCorrection": 0, "position": "below" },
///     "lines": [
///         { "label": "itemCount", "text": "1" },                 // the register's keyword, value right
///         { "text": "Zwrot do dnia: 2026-10-30", "align": "left" },
///         { "text": "Dziękujemy!", "doubleWidth": true }         // no label/align: as before
///     ],
///     "infoLines": [ "Zwrot lub wymiana …", "…" ],             // up to 3
///     "infoLinesAlign": "center"
/// } } }
/// </code>
/// The cashier and the register in the receipt footer come from the request itself: <c>cbUser</c>
/// and <c>cbTerminalID</c>.
/// <para>
/// On a POSNET register the codes, the cashier, the register and the info lines are one printout
/// configuration for the receipt that follows (<c>qrcode</c> + <c>ftrcfg</c>, sent before
/// <c>trinit</c>), and the text lines are the additional-lines phase after <c>trend fe0</c>
/// (<c>trftrln</c> … <c>trftrend</c>). The device allows one 1D and one 2D code per receipt, up to 60
/// lines of up to 40 characters and up to 3 info lines.
/// </para>
/// </summary>
public sealed record PosNetPrintout(
    string? Barcode,
    PosNetQrCode? QrCode,
    IReadOnlyList<PosNetFooterLine> Lines,
    IReadOnlyList<string>? InfoLines = null,
    bool InfoLinesCentered = false,
    string? Cashier = null,
    string? Register = null)
{
    /// <summary>Whether an <c>ftrcfg</c> has to be sent before the receipt.</summary>
    public bool HasFooterConfiguration => Barcode is not null || QrCode is not null || InfoLines is { Count: > 0 } || Cashier is not null || Register is not null;

    /// <summary>
    /// The info lines as <c>ftrcfg ln</c> carries them: one line each, with the centring code where
    /// asked, and a literal <c>&amp;</c> doubled — it introduces a formatting code there.
    /// </summary>
    public IReadOnlyList<string> FormattedInfoLines
        => (InfoLines ?? []).Select(line => (InfoLinesCentered ? "&c" : "") + line.Replace("&", "&&")).ToList();
}

public enum PosNetCode2dPosition
{
    /// <summary>ftrcfg bb1 — printed above the 1D code.</summary>
    Above = 1,

    /// <summary>ftrcfg bb2 — printed under the 1D code (default).</summary>
    Below = 2,
}

/// <param name="Data">The encoded content (UTF-8, at most 2000 bytes — the protocol's limit for tx).</param>
/// <param name="PixelSize">Side length in pixels of one code point (px, 2–8).</param>
/// <param name="ErrorCorrection">QR error correction level (el, 0 = L … 3 = H).</param>
public sealed record PosNetQrCode(string Data, int PixelSize, int ErrorCorrection, PosNetCode2dPosition Position);

/// <summary>
/// One additional line after the receipt (<c>trftrln</c>, POT-I-DEV-37 pp. 371–373): up to 40
/// characters, optionally double width/height. <paramref name="LineId"/> is the line type — 25 (no
/// keyword, text right-aligned behind a 15-character column), a keyword line 0–40, or 41 (text
/// left-aligned in normal size).
/// </summary>
public sealed record PosNetFooterLine(string Text, bool DoubleWidth, bool DoubleHeight, int LineId = PosNetFooterLine.PlainLineId)
{
    /// <summary>trftrln id25: the line without keyword, the text right-aligned behind a 15-character column.</summary>
    public const int PlainLineId = 25;

    /// <summary>
    /// trftrln id41: left-aligned text in normal size. The spec's table lists it without parameter,
    /// but a POSNET prints the na it is sent, without the keyword column (measured on a real device).
    /// </summary>
    public const int LeftAlignedLineId = 41;
}

/// <summary>
/// Reads the PL printout request out of the receipt request and validates it against the register's
/// limits — before any frame is sent, so a receipt that cannot be printed as requested fails without
/// leaving anything on the device.
/// </summary>
public static class PosNetPrintoutReader
{
    public const int MaxBarcodeLength = 30;
    public const int MaxLineLength = 40;
    public const int MaxLines = 60;
    public const int MaxQrCodeBytes = 2000;
    public const int MaxInfoLines = 3;
    public const int MaxCashierLength = 32;
    public const int MaxRegisterLength = 8;

    /// <summary>
    /// The register's keyword lines (trftrln id 0–40, p. 371), by device-neutral name. A keyword line
    /// prints the register's own label on the left and the line's text on the right. Ids 14 ("Nr
    /// pracownika", the same as 27) and 40 ("Ad.", ambiguous) are left out on purpose; 24 is a
    /// predefined text, 25 the plain line.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> KeywordLines = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["transactionNumber"] = 0,  // Nr transakcji
        ["points"] = 1,             // Punkty
        ["pointsTotal"] = 2,        // Suma punktów
        ["registrationNumber"] = 3, // Nr rejestracyjny
        ["lastName"] = 4,           // Nazwisko
        ["card"] = 5,               // Karta
        ["cardNumber"] = 6,         // Numer karty
        ["validUntil"] = 7,         // Ważna do
        ["cashier"] = 8,            // Kasjer
        ["cashierName"] = 9,        // Nazw. kasjera
        ["advancePayment"] = 10,    // Zaliczka
        ["currency"] = 11,          // Waluta
        ["exchangeRate"] = 12,      // Przelicznik
        ["orderNumber"] = 13,       // Nr zamówienia
        ["employeeName"] = 15,      // Nazw. pracownika
        ["accountBefore"] = 16,     // Konto przed tr.
        ["granted"] = 17,           // Przyznano
        ["used"] = 18,              // Wykorzystano
        ["accountAfter"] = 19,      // Konto po trans.
        ["regularCustomer"] = 20,   // Klient stały
        ["voucher"] = 21,           // Voucher
        ["voucherValue"] = 22,      // Wartość Voucher
        ["voucherPayment"] = 23,    // Zapłata Voucher
        ["itemCount"] = 26,         // Ilość sprzedanych towarów
        ["employeeNumber"] = 27,    // Numer pracownika
        ["customerNumber"] = 28,    // Numer klienta
        ["totalDiscounts"] = 29,    // Udzielono łącznie rabatów
        ["number"] = 30,            // Numer
        ["code"] = 31,              // Kod
        ["name"] = 32,              // Nazwa
        ["description"] = 33,       // Opis
        ["quantity"] = 34,          // Liczba
        ["customer"] = 35,          // Klient
        ["amount"] = 36,            // Kwota
        ["promotion"] = 37,         // Promocja
        ["info"] = 38,              // Info
        ["forInvoice"] = 39,        // Do faktury
    };

    /// <summary>
    /// The printout request, or null when there is nothing to print beyond the fiscal content. A
    /// present but invalid <c>PL.printout</c> — or a cashier or register the register cannot print —
    /// is a validation error; data that is not JSON, or JSON without a <c>PL.printout</c> object, is
    /// simply not a printout request (the field is an open extension point shared with other consumers).
    /// </summary>
    public static PosNetPrintout? Read(ReceiptRequest request)
    {
        var cashier = ReadCashier(request);
        var register = ReadRegister(request);

        if (PLReceiptCaseDataReader.ReadPLSection(request) is not { } pl
            || !PLReceiptCaseDataReader.TryGetPropertyIgnoreCase(pl, "printout", out var printout))
        {
            return cashier is null && register is null ? null : new PosNetPrintout(null, null, [], Cashier: cashier, Register: register);
        }

        if (printout.ValueKind != JsonValueKind.Object)
        {
            throw new PLValidationException("ftReceiptCaseData.PL.printout must be an object with barcode, qrCode, lines and/or infoLines.");
        }

        PrintoutPayload payload;
        try
        {
            payload = printout.Deserialize<PrintoutPayload>(SerializerOptions)
                ?? throw new PLValidationException("ftReceiptCaseData.PL.printout could not be read.");
        }
        catch (JsonException exception)
        {
            throw new PLValidationException($"ftReceiptCaseData.PL.printout is not a valid printout request: {exception.Message}");
        }

        return Validate(payload) with { Cashier = cashier, Register = register };
    }

    /// <summary>
    /// The cashier printed in the receipt footer (<c>ftrcfg cc</c>), from <c>cbUser</c>. Only a name
    /// can be printed: a string (or a number) is taken; a structured user object is not a name and is
    /// left out, as the rest of the middleware reads cbUser. Too long is an integration error.
    /// </summary>
    private static string? ReadCashier(ReceiptRequest request)
    {
        var cashier = ScalarText(request.cbUser);
        if (string.IsNullOrWhiteSpace(cashier))
        {
            return null;
        }
        cashier = cashier.Trim();
        RequireFits(cashier, MaxCashierLength, "cbUser", "the cashier name in the receipt footer (ftrcfg cc)");
        return cashier;
    }

    /// <summary>The register number printed in the receipt footer (<c>ftrcfg cn</c>), from <c>cbTerminalID</c>.</summary>
    private static string? ReadRegister(ReceiptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.cbTerminalID))
        {
            return null;
        }
        var register = request.cbTerminalID.Trim();
        RequireFits(register, MaxRegisterLength, "cbTerminalID", "the register number in the receipt footer (ftrcfg cn)");
        return register;
    }

    /// <summary>
    /// A value the register would refuse with error 10 (PROTO_ERR_DATA_LENGTH) is an integration
    /// error the POS has to fix — refused here, naming the field, before anything is sent.
    /// </summary>
    private static void RequireFits(string value, int maxLength, string field, string usage)
    {
        if (value.Length > maxLength)
        {
            throw new PLValidationException($"{field} '{value}' has {value.Length} characters — a POSNET register prints at most {maxLength} as {usage}. Send a shorter {field}.");
        }
        if (PosNetText.ContainsFramingCharacter(value))
        {
            throw new PLValidationException($"{field} '{value}' contains a control character, which a POSNET register cannot print.");
        }
    }

    private static string? ScalarText(object? value) => value switch
    {
        null => null,
        string text => text,
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement { ValueKind: JsonValueKind.Number } element => element.GetRawText(),
        int or long or decimal or double => Convert.ToString(value, CultureInfo.InvariantCulture),
        _ => null,
    };

    private static PosNetPrintout Validate(PrintoutPayload payload)
    {
        string? barcode = null;
        if (!string.IsNullOrWhiteSpace(payload.Barcode))
        {
            barcode = payload.Barcode.Trim();
            if (barcode.Length > MaxBarcodeLength)
            {
                throw new PLValidationException($"The footer barcode has {barcode.Length} characters — a POSNET register prints at most {MaxBarcodeLength} (ftrcfg bc).");
            }
            foreach (var c in barcode)
            {
                if (!char.IsAsciiLetterOrDigit(c))
                {
                    throw new PLValidationException($"The footer barcode '{barcode}' must consist of ASCII letters and digits only (ftrcfg bc).");
                }
            }
        }

        PosNetQrCode? qrCode = null;
        if (payload.QrCode is { } qr)
        {
            if (string.IsNullOrEmpty(qr.Data))
            {
                throw new PLValidationException("The footer qrCode needs a non-empty 'data' value.");
            }
            var bytes = Encoding.UTF8.GetByteCount(qr.Data);
            if (bytes > MaxQrCodeBytes)
            {
                throw new PLValidationException($"The footer qrCode data is {bytes} bytes — a POSNET register encodes at most {MaxQrCodeBytes} (qrcode tx).");
            }
            var pixelSize = qr.PixelSize ?? 2;
            if (pixelSize is < 2 or > 8)
            {
                throw new PLValidationException($"The footer qrCode pixelSize {pixelSize} is out of range (2–8).");
            }
            var errorCorrection = qr.ErrorCorrection ?? 0;
            if (errorCorrection is < 0 or > 3)
            {
                throw new PLValidationException($"The footer qrCode errorCorrection {errorCorrection} is out of range (0 = L … 3 = H).");
            }
            var position = qr.Position?.Trim().ToLowerInvariant() switch
            {
                null or "" or "below" => PosNetCode2dPosition.Below,
                "above" => PosNetCode2dPosition.Above,
                var other => throw new PLValidationException($"The footer qrCode position '{other}' is not supported — use 'above' or 'below' (relative to the barcode)."),
            };
            qrCode = new PosNetQrCode(qr.Data, pixelSize, errorCorrection, position);
        }

        var lines = new List<PosNetFooterLine>();
        foreach (var line in payload.Lines ?? [])
        {
            if (lines.Count == MaxLines)
            {
                throw new PLValidationException($"More than {MaxLines} additional lines were requested — a POSNET register prints at most {MaxLines} after a receipt (trftrln).");
            }
            var text = (line.Text ?? "").TrimEnd();
            if (text.Length > MaxLineLength)
            {
                throw new PLValidationException($"The additional line '{text}' has {text.Length} characters — a POSNET register prints at most {MaxLineLength} per line (trftrln na).");
            }
            if (PosNetText.ContainsFramingCharacter(text))
            {
                throw new PLValidationException($"The additional line '{text}' contains a protocol framing character.");
            }
            var lineId = LineIdOf(line, text);
            // A left-aligned line (id41) reads '&' as a formatting code, like the info lines: a literal one is doubled.
            lines.Add(new PosNetFooterLine(lineId == PosNetFooterLine.LeftAlignedLineId ? text.Replace("&", "&&") : text,
                line.DoubleWidth ?? false, line.DoubleHeight ?? false, lineId));
        }

        var infoLines = new List<string>();
        foreach (var infoLine in payload.InfoLines ?? [])
        {
            if (infoLines.Count == MaxInfoLines)
            {
                throw new PLValidationException($"More than {MaxInfoLines} infoLines were requested — a POSNET register holds at most {MaxInfoLines} info lines per receipt (ftrcfg ln).");
            }
            var text = (infoLine ?? "").TrimEnd();
            if (text.Length > MaxLineLength)
            {
                throw new PLValidationException($"The info line '{text}' has {text.Length} characters — a POSNET register prints at most {MaxLineLength} per line (ftrcfg ln).");
            }
            if (PosNetText.ContainsFramingCharacter(text))
            {
                throw new PLValidationException($"The info line '{text}' contains a control character; send one entry per line.");
            }
            infoLines.Add(text);
        }

        var centered = payload.InfoLinesAlign?.Trim().ToLowerInvariant() switch
        {
            null or "" or "left" => false,
            "center" => true,
            var other => throw new PLValidationException($"infoLinesAlign '{other}' is not supported — use 'left' or 'center'."),
        };

        return new PosNetPrintout(barcode, qrCode, lines, infoLines, centered);
    }

    /// <summary>The trftrln line type a line asks for: a keyword line by label, the left-aligned line, or the plain line.</summary>
    private static int LineIdOf(LinePayload line, string text)
    {
        var align = line.Align?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(line.Label))
        {
            if (!string.IsNullOrEmpty(align))
            {
                throw new PLValidationException($"The additional line '{text}' has both a label and an align — a keyword line places its text itself.");
            }
            return KeywordLines.TryGetValue(line.Label.Trim(), out var keywordId)
                ? keywordId
                : throw new PLValidationException($"The additional line label '{line.Label}' is not a POSNET keyword line. Known labels: {string.Join(", ", KeywordLines.Keys)}.");
        }

        return align switch
        {
            null or "" => PosNetFooterLine.PlainLineId,
            "left" when line.DoubleWidth == true || line.DoubleHeight == true
                => throw new PLValidationException($"The additional line '{text}' is left-aligned and double width/height — a POSNET register prints left-aligned lines in normal size only (trftrln id41)."),
            "left" => PosNetFooterLine.LeftAlignedLineId,
            _ => throw new PLValidationException($"The additional line align '{line.Align}' is not supported — use 'left', or leave it out for the plain line."),
        };
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private sealed class PrintoutPayload
    {
        public string? Barcode { get; set; }

        public QrCodePayload? QrCode { get; set; }

        public List<LinePayload>? Lines { get; set; }

        public List<string?>? InfoLines { get; set; }

        public string? InfoLinesAlign { get; set; }
    }

    private sealed class QrCodePayload
    {
        public string? Data { get; set; }

        public int? PixelSize { get; set; }

        public int? ErrorCorrection { get; set; }

        public string? Position { get; set; }
    }

    private sealed class LinePayload
    {
        public string? Text { get; set; }

        public bool? DoubleWidth { get; set; }

        public bool? DoubleHeight { get; set; }

        public string? Label { get; set; }

        public string? Align { get; set; }
    }
}
