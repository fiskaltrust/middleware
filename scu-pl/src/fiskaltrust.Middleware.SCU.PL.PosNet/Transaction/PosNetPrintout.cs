using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.PosNet.Protocol;

namespace fiskaltrust.Middleware.SCU.PL.PosNet.Transaction;

/// <summary>
/// What a POS asks to be printed on the fiscal receipt beyond the fiscal content — carried in
/// <c>ftReceiptCaseData</c> as <c>{ "PL": { "printout": { … } } }</c>:
/// <code>
/// { "PL": { "printout": {
///     "barcode": "1234567890",                                   // 1D code in the receipt footer
///     "qrCode":  { "data": "https://…", "pixelSize": 2, "errorCorrection": 0, "position": "below" },
///     "lines":   [ { "text": "Dziękujemy!", "doubleWidth": true, "doubleHeight": false } ]
/// } } }
/// </code>
/// On a POSNET register the footer codes are a printout configuration valid for the next receipt
/// (<c>qrcode</c> + <c>ftrcfg bc/bb</c>, sent before <c>trinit</c>), and the text lines are the
/// additional-lines phase after <c>trend fe0</c> (<c>trftrln id25</c> … <c>trftrend</c>). The
/// device allows one 1D and one 2D code per receipt, up to 60 lines of up to 40 characters.
/// </summary>
public sealed record PosNetPrintout(string? Barcode, PosNetQrCode? QrCode, IReadOnlyList<PosNetFooterLine> Lines)
{
    public bool HasFooterCodes => Barcode is not null || QrCode is not null;
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

/// <summary>One additional line after the receipt (trftrln id25): up to 40 characters, optionally double width/height.</summary>
public sealed record PosNetFooterLine(string Text, bool DoubleWidth, bool DoubleHeight);

/// <summary>
/// Reads the PL printout request out of <c>ftReceiptCaseData</c> and validates it against the
/// register's limits — before any frame is sent, so a receipt that cannot be printed as requested
/// fails without leaving anything on the device.
/// </summary>
public static class PosNetPrintoutReader
{
    public const int MaxBarcodeLength = 30;
    public const int MaxLineLength = 40;
    public const int MaxLines = 60;
    public const int MaxQrCodeBytes = 2000;

    /// <summary>
    /// The printout request, or null when ftReceiptCaseData carries none. A present but invalid
    /// <c>PL.printout</c> is a validation error; data that is not JSON, or JSON without a
    /// <c>PL.printout</c> object, is simply not a printout request (the field is an open extension
    /// point shared with other consumers).
    /// </summary>
    public static PosNetPrintout? Read(ReceiptRequest request)
    {
        var json = ToJson(request.ftReceiptCaseData);
        if (json is null)
        {
            return null;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !TryGetPropertyIgnoreCase(document.RootElement, "PL", out var pl)
                || pl.ValueKind != JsonValueKind.Object
                || !TryGetPropertyIgnoreCase(pl, "printout", out var printout))
            {
                return null;
            }

            if (printout.ValueKind != JsonValueKind.Object)
            {
                throw new PLValidationException("ftReceiptCaseData.PL.printout must be an object with barcode, qrCode and/or lines.");
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

            return Validate(payload);
        }
    }

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
            lines.Add(new PosNetFooterLine(text, line.DoubleWidth ?? false, line.DoubleHeight ?? false));
        }

        return new PosNetPrintout(barcode, qrCode, lines);
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>ftReceiptCaseData is an open object on the wire; a JSON string is accepted as well.</summary>
    private static string? ToJson(object? receiptCaseData)
    {
        switch (receiptCaseData)
        {
            case null:
                return null;
            case string text:
                return string.IsNullOrWhiteSpace(text) ? null : text;
            case JsonElement element:
                return element.ValueKind == JsonValueKind.Null ? null : element.GetRawText();
            default:
                return JsonSerializer.Serialize(receiptCaseData);
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private sealed class PrintoutPayload
    {
        public string? Barcode { get; set; }

        public QrCodePayload? QrCode { get; set; }

        public List<LinePayload>? Lines { get; set; }
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
    }
}
