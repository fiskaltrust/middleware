using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// The canonical form of a receipt request that every French SCU hashes and signs (market-fr ADR
/// 0013, market-fr#225). It binds a signature to the request the POS sent, independently of how
/// the queue serializes or stores it, so TaxAuditorTool or any third party can recompute the hash
/// from the archived request.
/// </summary>
/// <remarks>
/// Version 1 covers the members of the ifPOS v2 <see cref="ReceiptRequest"/>, <see cref="ChargeItem"/>
/// and <see cref="PayItem"/> listed in ADR 0013, and nothing else: a member added to the contract later
/// is not hashed until a new version is defined. Every rule here is part of the certified signed
/// content, so a change means a new version, never an edit of this one.
/// <list type="bullet">
/// <item>UTF-8 without BOM, no whitespace between tokens.</item>
/// <item>Object members in ordinal order of their UTF-16 code units (RFC 8785 §3.2.3).</item>
/// <item>A member whose value is null is left out. So are empty <c>cbChargeItems</c> and <c>cbPayItems</c>.</item>
/// <item>Members that cannot be null in the contract are always written, with their contract default when absent.</item>
/// <item>Numbers as their exact value in plain decimal notation: no exponent, no leading zeros, no trailing fraction zeros, no <c>-0</c>.</item>
/// <item>Moments in UTC as <c>yyyy-MM-ddTHH:mm:ss.fffffffZ</c>; a moment without offset is taken as UTC.</item>
/// <item>Identifiers in lowercase <c>8-4-4-4-12</c> form, currencies as their ISO 4217 code.</item>
/// <item>Strings escaped as RFC 8785 §3.2.2.2 does it; lone surrogates are rejected.</item>
/// <item>Free-form members (<c>*CaseData</c>, <c>cbUser</c>, <c>cbArea</c>, <c>cbCustomer</c>, <c>cbSettlement</c>) keep their JSON structure under the same rules.</item>
/// </list>
/// </remarks>
public static class FRCanonicalRequest
{
    public const int Version = 1;

    /// <summary>Longest number, in characters, the canonical form writes; a longer one is rejected.</summary>
    private const int MaxNumberLength = 1000;

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static readonly Regex JsonNumber = new(@"^(-?)(\d+)(?:\.(\d+))?(?:[eE]([+-]?\d+))?$", RegexOptions.CultureInvariant);

    /// <summary>The canonical bytes of <paramref name="request"/>.</summary>
    /// <exception cref="ArgumentException">The request holds a value version 1 cannot write.</exception>
    public static byte[] Serialize(ReceiptRequest request)
    {
        var builder = new StringBuilder();
        WriteRequest(builder, request);
        try
        {
            return Utf8.GetBytes(builder.ToString());
        }
        catch (EncoderFallbackException ex)
        {
            throw new ArgumentException("The request holds a string with a lone surrogate, which has no UTF-8 encoding.", nameof(request), ex);
        }
    }

    /// <summary>Base64 (RFC 4648 §4, padded) SHA-256 of the canonical bytes.</summary>
    public static string Hash(ReceiptRequest request) => Convert.ToBase64String(SHA256.HashData(Serialize(request)));

    /// <summary>The value an SCU signs: the version, a colon, then <see cref="Hash"/>.</summary>
    public static string SignedField(ReceiptRequest request) => $"{Version.ToString(CultureInfo.InvariantCulture)}:{Hash(request)}";

    private static void WriteRequest(StringBuilder builder, ReceiptRequest request)
    {
        var members = new List<(string, Action<StringBuilder>?)>
        {
            ("Currency", b => WriteString(b, request.Currency.ToString())),
            ("DecimalPrecisionMultiplier", b => b.Append(request.DecimalPrecisionMultiplier.ToString(CultureInfo.InvariantCulture))),
            ("cbArea", FreeForm(request.cbArea)),
            ("cbChargeItems", request.cbChargeItems is { Count: > 0 } chargeItems ? b => WriteArray(b, chargeItems, WriteChargeItem) : null),
            ("cbCustomer", FreeForm(request.cbCustomer)),
            ("cbPayItems", request.cbPayItems is { Count: > 0 } payItems ? b => WriteArray(b, payItems, WritePayItem) : null),
            ("cbPreviousReceiptReference", PreviousReceiptReference(request.cbPreviousReceiptReference)),
            ("cbReceiptAmount", Dec(request.cbReceiptAmount)),
            ("cbReceiptMoment", At(request.cbReceiptMoment)),
            ("cbReceiptReference", Str(request.cbReceiptReference)),
            ("cbSettlement", FreeForm(request.cbSettlement)),
            ("cbTerminalID", Str(request.cbTerminalID)),
            ("cbUser", FreeForm(request.cbUser)),
            ("ftCashBoxID", Id(request.ftCashBoxID)),
            ("ftPosSystemId", Id(request.ftPosSystemId)),
            ("ftQueueID", Id(request.ftQueueID)),
            ("ftReceiptCase", b => b.Append(((ulong) request.ftReceiptCase).ToString(CultureInfo.InvariantCulture))),
            ("ftReceiptCaseData", FreeForm(request.ftReceiptCaseData)),
        };
        WriteObject(builder, members);
    }

    private static void WriteChargeItem(StringBuilder builder, ChargeItem item) => WriteObject(builder, new List<(string, Action<StringBuilder>?)>
    {
        ("AccountNumber", Str(item.AccountNumber)),
        ("Amount", Dec(item.Amount)),
        ("CostCenter", Str(item.CostCenter)),
        ("Currency", b => WriteString(b, item.Currency.ToString())),
        ("DecimalPrecisionMultiplier", b => b.Append(item.DecimalPrecisionMultiplier.ToString(CultureInfo.InvariantCulture))),
        ("Description", Str(item.Description)),
        ("Moment", item.Moment is { } moment ? At(moment) : null),
        ("Position", Dec(item.Position)),
        ("ProductBarcode", Str(item.ProductBarcode)),
        ("ProductGroup", Str(item.ProductGroup)),
        ("ProductNumber", Str(item.ProductNumber)),
        ("Quantity", Dec(item.Quantity)),
        ("Unit", Str(item.Unit)),
        ("UnitPrice", Dec(item.UnitPrice)),
        ("UnitQuantity", Dec(item.UnitQuantity)),
        ("VATAmount", Dec(item.VATAmount)),
        ("VATRate", Dec(item.VATRate)),
        ("ftChargeItemCase", b => b.Append(((ulong) item.ftChargeItemCase).ToString(CultureInfo.InvariantCulture))),
        ("ftChargeItemCaseData", FreeForm(item.ftChargeItemCaseData)),
        ("ftChargeItemId", Id(item.ftChargeItemId)),
    });

    private static void WritePayItem(StringBuilder builder, PayItem item) => WriteObject(builder, new List<(string, Action<StringBuilder>?)>
    {
        ("AccountNumber", Str(item.AccountNumber)),
        ("Amount", Dec(item.Amount)),
        ("CostCenter", Str(item.CostCenter)),
        ("Currency", b => WriteString(b, item.Currency.ToString())),
        ("DecimalPrecisionMultiplier", b => b.Append(item.DecimalPrecisionMultiplier.ToString(CultureInfo.InvariantCulture))),
        ("Description", Str(item.Description)),
        ("MoneyBarcode", Str(item.MoneyBarcode)),
        ("MoneyGroup", Str(item.MoneyGroup)),
        ("MoneyNumber", Str(item.MoneyNumber)),
        ("Moment", item.Moment is { } moment ? At(moment) : null),
        ("Position", Dec(item.Position)),
        ("Quantity", Dec(item.Quantity)),
        ("ftPayItemCase", b => b.Append(((ulong) item.ftPayItemCase).ToString(CultureInfo.InvariantCulture))),
        ("ftPayItemCaseData", FreeForm(item.ftPayItemCaseData)),
        ("ftPayItemId", Id(item.ftPayItemId)),
    });

    private static Action<StringBuilder>? Str(string? value) => value is null ? null : b => WriteString(b, value);

    private static Action<StringBuilder>? Dec(decimal? value) => value is { } v ? b => b.Append(Number(v.ToString(CultureInfo.InvariantCulture))) : null;

    private static Action<StringBuilder>? Id(Guid? value) => value is { } v ? b => WriteString(b, v.ToString("D")) : null;

    private static Action<StringBuilder> At(DateTime value)
    {
        // A moment without offset (DateTimeKind.Unspecified) is taken as UTC, never as the machine's local time.
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
        return b => WriteString(b, utc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffff'Z'", CultureInfo.InvariantCulture));
    }

    private static Action<StringBuilder>? PreviousReceiptReference(cbPreviousReceiptReference? value) => value?.Match<Action<StringBuilder>>(
        single => b => WriteString(b, single),
        group => b => WriteArray(b, group, (bb, s) => { if (s is null) { bb.Append("null"); } else { WriteString(bb, s); } }));

    private static Action<StringBuilder>? FreeForm(object? value)
    {
        if (value is null)
        {
            return null;
        }
        var element = ToJsonElement(value);
        return element.ValueKind == JsonValueKind.Null ? null : b => WriteElement(b, element);
    }

    private static JsonElement ToJsonElement(object value) => value switch
    {
        JsonElement element => element,
        JsonDocument document => document.RootElement,
        // Newtonsoft's JToken (what a Newtonsoft-deserialized request holds) goes through its own JSON
        // text, so its numbers keep their literal. Matched by name: this assembly does not reference Newtonsoft.
        _ when IsJToken(value.GetType()) => JsonDocument.Parse(value.ToString()!).RootElement,
        _ => JsonSerializer.SerializeToElement(value, value.GetType()),
    };

    private static bool IsJToken(Type? type)
    {
        for (; type is not null; type = type.BaseType)
        {
            if (type.FullName == "Newtonsoft.Json.Linq.JToken")
            {
                return true;
            }
        }
        return false;
    }

    private static void WriteElement(StringBuilder builder, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var members = new List<(string, Action<StringBuilder>?)>();
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                    {
                        throw new ArgumentException($"The free-form member \"{property.Name}\" appears twice in the same object.");
                    }
                    var value = property.Value;
                    members.Add((property.Name, value.ValueKind == JsonValueKind.Null ? null : b => WriteElement(b, value)));
                }
                WriteObject(builder, members);
                break;
            case JsonValueKind.Array:
                WriteArray(builder, element.EnumerateArray().ToList(), WriteElement);
                break;
            case JsonValueKind.String:
                WriteString(builder, element.GetString()!);
                break;
            case JsonValueKind.Number:
                builder.Append(Number(element.GetRawText()));
                break;
            case JsonValueKind.True:
                builder.Append("true");
                break;
            case JsonValueKind.False:
                builder.Append("false");
                break;
            default:
                builder.Append("null");
                break;
        }
    }

    private static void WriteObject(StringBuilder builder, List<(string Name, Action<StringBuilder>? Write)> members)
    {
        builder.Append('{');
        var first = true;
        foreach (var (name, write) in members.Where(m => m.Write is not null).OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            if (!first)
            {
                builder.Append(',');
            }
            first = false;
            WriteString(builder, name);
            builder.Append(':');
            write!(builder);
        }
        builder.Append('}');
    }

    private static void WriteArray<T>(StringBuilder builder, IEnumerable<T> items, Action<StringBuilder, T> write)
    {
        builder.Append('[');
        var first = true;
        foreach (var item in items)
        {
            if (!first)
            {
                builder.Append(',');
            }
            first = false;
            if (item is null)
            {
                builder.Append("null");
            }
            else
            {
                write(builder, item);
            }
        }
        builder.Append(']');
    }

    /// <summary>RFC 8785 §3.2.2.2: only the quote, the backslash and the controls below U+0020 are escaped.</summary>
    private static void WriteString(StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        builder.Append("\\u").Append(((int) c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }
                    break;
            }
        }
        builder.Append('"');
    }

    /// <summary>The exact value of a JSON number literal in plain decimal notation.</summary>
    internal static string Number(string literal)
    {
        var match = JsonNumber.Match(literal);
        if (!match.Success)
        {
            throw new ArgumentException($"\"{literal}\" is not a JSON number.");
        }
        var negative = match.Groups[1].Length > 0;
        var fraction = match.Groups[3].Value;
        var digits = (match.Groups[2].Value + fraction).TrimStart('0');
        if (!long.TryParse(match.Groups[4].Success ? match.Groups[4].Value : "0", NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var exponent) || Math.Abs(exponent) > MaxNumberLength)
        {
            throw new ArgumentException($"The number \"{literal}\" is out of the range the canonical form writes.");
        }
        if (digits.Length == 0)
        {
            return "0";
        }
        // value = digits × 10^scale
        var scale = exponent - fraction.Length;
        var trimmed = digits.TrimEnd('0');
        scale += digits.Length - trimmed.Length;
        digits = trimmed;

        string plain;
        if (scale >= 0)
        {
            plain = digits + new string('0', checked((int) Math.Min(scale, MaxNumberLength + 1)));
        }
        else
        {
            var point = digits.Length + scale;
            plain = point > 0
                ? digits[..(int) point] + "." + digits[(int) point..]
                : "0." + new string('0', checked((int) Math.Min(-point, MaxNumberLength + 1))) + digits;
        }
        if (plain.Length > MaxNumberLength)
        {
            throw new ArgumentException($"The number \"{literal}\" is longer than {MaxNumberLength} characters in plain notation.");
        }
        return negative ? "-" + plain : plain;
    }
}
