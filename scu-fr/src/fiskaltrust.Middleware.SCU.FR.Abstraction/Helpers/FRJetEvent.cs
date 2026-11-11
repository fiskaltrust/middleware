using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// A technical event of the NF525 technical event journal (JET, Infocert R19 v4.2), as the POS
/// sends it: a <c>ProtocolTechnicalEvent 0x3001</c> receipt whose <c>ftReceiptCaseData.FR</c> carries
/// the event code, its label and the optional contextual information:
/// <c>{"FR":{"Code":"90","Message":"…","Information":"…"}}</c>. The keys are the ones v1.3 used and
/// TaxAuditorTool reads, moved under the market block like all French case data (market-fr#248, the
/// v2 contract of market-fr#243); property names are case-insensitive (market-fr#42 was closed
/// without introducing <c>JET-*</c> names). The operator is <c>cbUser</c>.
/// <para>
/// One JET behaviour for every certification body (decided 2026-11-07, market-fr#200): the queue
/// refuses a malformed event before any SCU signs it, whichever SCU is configured. The InfoCert SCU
/// also reads it to sign the R19 content; the LNE and self-certified SCUs sign it with their data set.
/// </para>
/// </summary>
public sealed record FRJetEvent(int Code, string Label, string? Information, string? Operator)
{
    /// <summary>
    /// The event code for a change of the continuous-sequence mode. R19 v4 gave code 10 this
    /// meaning; 15 is only an alternative for software that kept 10 for JET access, which v2 never
    /// did (decided 2026-11-04, market-fr#200). 15 stays a valid code.
    /// </summary>
    public const int ContinuousSequenceModeChange = 10;

    /// <summary>The first editor-specific code; every code from here on is the editor's own (R19 v4.2).</summary>
    public const int FirstEditorCode = 900;

    /// <summary>The codes R19 v4.2 defines, 300 included (historic, replaced by 270 but still listed).</summary>
    public static readonly IReadOnlySet<int> R19Codes = new HashSet<int>
    {
        ContinuousSequenceModeChange, 15,
        20, 30, 40, 50, 60, 70, 80, 90, 95, 100, 105, 110, 120, 123, 125, 126, 127, 128,
        130, 140, 150, 155, 156, 160, 170, 180, 190, 200, 205, 210, 220, 230, 240, 250, 255,
        260, 270, 280, 290, 295, 300, 320, 323, 324, 325, 326, 327, 328, 329, 330,
        400, 410, 420, 430, 440, 450,
    };

    /// <summary>True for a code R19 v4.2 defines, or an editor code (900 and above).</summary>
    public static bool IsValidCode(int code) => R19Codes.Contains(code) || code >= FirstEditorCode;

    /// <summary>
    /// True for a receipt that is a JET event: a non-training <c>ProtocolTechnicalEvent 0x3001</c>.
    /// The other log cases (0x3000, 0x3002, 0x3003) stay on chain L with the generic content.
    /// </summary>
    public static bool IsJetEvent(ReceiptRequest request)
        => request.ftReceiptCase.Case() == ReceiptCase.ProtocolTechnicalEvent0x3001
           && !request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training);

    /// <summary>
    /// Reads the JET event from the receipt. Fails with a reason when the case data is missing,
    /// not an object, or has no valid R19 code; the label is mandatory too (JET-EVT-LIB).
    /// </summary>
    public static bool TryRead(ReceiptRequest request, out FRJetEvent? jetEvent, out string? error)
    {
        jetEvent = null;
        var root = MarketBlock(AsObject(request.ftReceiptCaseData));
        if (root is null)
        {
            error = "ftReceiptCaseData.FR must be an object {\"Code\":…,\"Message\":…,\"Information\":…} describing the technical event.";
            return false;
        }

        var codeText = Property(root.Value, "Code");
        if (codeText is null || !int.TryParse(codeText, out var code))
        {
            error = $"ftReceiptCaseData.FR.Code must be an NF525 technical event code (R19 v4.2), but was '{codeText}'.";
            return false;
        }

        if (!IsValidCode(code))
        {
            error = $"ftReceiptCaseData.FR.Code {code} is not a technical event code of R19 v4.2 and not an editor code (>= {FirstEditorCode}).";
            return false;
        }

        var label = Property(root.Value, "Message");
        if (string.IsNullOrWhiteSpace(label))
        {
            error = "ftReceiptCaseData.FR.Message must hold the label of the technical event (JET-EVT-LIB).";
            return false;
        }

        jetEvent = new FRJetEvent(code, label, NullIfEmpty(Property(root.Value, "Information")), OperatorOf(request.cbUser));
        error = null;
        return true;
    }

    private static JsonElement? AsObject(object? caseData)
    {
        try
        {
            var element = caseData switch
            {
                null => (JsonElement?) null,
                JsonElement e when e.ValueKind == JsonValueKind.String => JsonDocument.Parse(e.GetString()!).RootElement.Clone(),
                JsonElement e => e,
                string s => JsonDocument.Parse(s).RootElement.Clone(),
                _ => JsonSerializer.SerializeToElement(caseData),
            };
            return element is { ValueKind: JsonValueKind.Object } ? element : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The <c>FR</c> object of the case data, or null when there is none.</summary>
    private static JsonElement? MarketBlock(JsonElement? caseData)
    {
        if (caseData is null)
        {
            return null;
        }

        foreach (var property in caseData.Value.EnumerateObject())
        {
            if (string.Equals(property.Name, "FR", StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.Object ? property.Value : null;
            }
        }

        return null;
    }

    private static string? Property(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    _ => property.Value.GetRawText(),
                };
            }
        }

        return null;
    }

    private static string? OperatorOf(object? cbUser) => cbUser switch
    {
        null => null,
        string s => NullIfEmpty(s),
        JsonElement { ValueKind: JsonValueKind.String } e => NullIfEmpty(e.GetString()),
        JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
        JsonElement e => e.GetRawText(),
        _ => JsonSerializer.Serialize(cbUser),
    };

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
