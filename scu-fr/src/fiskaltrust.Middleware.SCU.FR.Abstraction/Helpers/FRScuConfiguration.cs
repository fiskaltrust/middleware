using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// Reads and checks the configuration an FR SCU is started with. The launcher hands a dictionary whose values may
/// be strings, numbers, <see cref="JsonElement"/>s or Newtonsoft <c>JToken</c>s; every value the SCU uses is a
/// string, so scalars are read as their text and nested objects are kept as JSON (a <c>JObject</c> is never walked
/// by System.Text.Json, whose serializer follows its Parent/Root cycle).
/// </summary>
public static class FRScuConfiguration
{
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    public static T Read<T>(Dictionary<string, object> configuration) where T : new()
    {
        var json = new JsonObject();
        foreach (var pair in configuration)
        {
            if (Node(pair.Value) is { } node)
            {
                json[pair.Key] = node;
            }
        }
        return json.Deserialize<T>(ReadOptions) ?? new T();
    }

    private static JsonNode? Node(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case string text:
                return JsonValue.Create(text);
            case bool flag:
                return JsonValue.Create(flag ? "true" : "false");
            case JsonElement element:
                return element.ValueKind switch
                {
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    JsonValueKind.String => JsonValue.Create(element.GetString()),
                    JsonValueKind.Object or JsonValueKind.Array => JsonNode.Parse(element.GetRawText()),
                    _ => JsonValue.Create(element.GetRawText()),
                };
            case IConvertible convertible:
                return JsonValue.Create(convertible.ToString(CultureInfo.InvariantCulture));
        }

        if (FRJsonValues.IsJToken(value.GetType()))
        {
            // A JValue holds its CLR value; a JObject or JArray is read through its own JSON text.
            var inner = value.GetType().GetProperty("Value")?.GetValue(value);
            return inner is not null || value.GetType().Name == "JValue" ? Node(inner) : JsonNode.Parse(value.ToString()!);
        }

        try
        {
            return JsonSerializer.SerializeToNode(value, value.GetType());
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            // A value the SCU does not read and cannot serialize is no reason to refuse the configuration.
            return null;
        }
    }

    /// <summary>
    /// Refuses a configured serial that is not the serial of the configured certificate (Ref LNE 1.8 Ex. 9, [08]):
    /// the verifier picks the certificate by the signed serial. Hex digits are compared in any case and without
    /// separators or leading zeros. Nothing is checked without a certificate, or with one that does not parse: signing
    /// an archive reports that (<see cref="FRHashSigning.SigningCertificate"/>).
    /// </summary>
    public static void CheckSerialMatchesCertificate(string scuName, string? certificateBase64, string? serial)
    {
        if (string.IsNullOrWhiteSpace(certificateBase64))
        {
            return;
        }

        string certificateSerial;
        try
        {
            using var certificate = new X509Certificate2(Convert.FromBase64String(certificateBase64));
            certificateSerial = certificate.SerialNumber;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return;
        }

        if (NormalizeSerial(serial) != NormalizeSerial(certificateSerial))
        {
            throw new FRValidationException($"The {scuName} SCU's CertificateSerialNumber '{serial}' is not the serial of its CertificateBase64 ({certificateSerial}).");
        }
    }

    private static string NormalizeSerial(string? serial)
    {
        var builder = new StringBuilder();
        foreach (var c in serial ?? "")
        {
            if (Uri.IsHexDigit(c))
            {
                builder.Append(char.ToUpperInvariant(c));
            }
            else if (c is not (':' or ' ' or '-'))
            {
                return serial!;
            }
        }
        return builder.ToString().TrimStart('0');
    }
}

/// <summary>Recognises Newtonsoft values without referencing Newtonsoft.</summary>
public static class FRJsonValues
{
    public static bool IsJToken(Type? type)
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
}
