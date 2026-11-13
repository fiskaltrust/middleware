using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;

/// <summary>
/// The compact JWS (RFC 7515) every French SCU's receipt QR code is: <c>header.payload.signature</c>, each part Base64Url
/// without padding. The protected header is <c>{"alg":…,"typ":"JWT","kid":"&lt;certificate serial&gt;"}</c>, keys in that
/// order, <c>kid</c> left out when no serial is configured (market-fr#221). The payload is the UTF-8 signed string — the
/// R13/R12/R19 string or the 25-field data set — never JSON claims. The signature segment is what the next entry of the
/// chain carries (ADR 0001, <see cref="FRChaining.ChainedSignatureOf"/>).
/// </summary>
/// <remarks>
/// Shared so that every SCU builds, and every reader takes apart, the same envelope; only the algorithm and the key
/// differ. The signing input is the ASCII of <c>header.payload</c> (RFC 7515 §5.1), so the hash covers the Base64Url form
/// of the signed string behind the header, which a verifier decodes back exactly.
/// </remarks>
public static class FRJws
{
    /// <summary>ECDSA P-256 with SHA-256, the signature in raw R‖S form (RFC 7518 §3.4).</summary>
    public const string ES256 = "ES256";

    /// <summary>The protected header, Base64Url: <c>{"alg":"&lt;alg&gt;","typ":"JWT"[,"kid":"&lt;keyId&gt;"]}</c>.</summary>
    public static string Header(string algorithm, string? keyId)
    {
        var json = string.IsNullOrEmpty(keyId)
            ? $"{{\"alg\":{JsonSerializer.Serialize(algorithm)},\"typ\":\"JWT\"}}"
            : $"{{\"alg\":{JsonSerializer.Serialize(algorithm)},\"typ\":\"JWT\",\"kid\":{JsonSerializer.Serialize(keyId)}}}";
        return FRChaining.ToBase64Url(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>Signs <paramref name="payload"/> into a compact JWS with <paramref name="sign"/> over the signing input.</summary>
    /// <returns>The compact JWS and its signature segment.</returns>
    public static (string jws, string signature) Sign(string payload, string algorithm, string? keyId, Func<byte[], byte[]> sign)
    {
        var signingInput = $"{Header(algorithm, keyId)}.{FRChaining.ToBase64Url(Encoding.UTF8.GetBytes(payload))}";
        var signature = FRChaining.ToBase64Url(sign(Encoding.ASCII.GetBytes(signingInput)));
        return ($"{signingInput}.{signature}", signature);
    }

    /// <summary>An ES256 JWS signed with <paramref name="key"/>.</summary>
    public static (string jws, string signature) SignES256(ECDsa key, string payload, string? keyId)
        => Sign(payload, ES256, keyId, input => key.SignData(input, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));

    /// <summary>True when <paramref name="qrCode"/> has the three segments of a compact JWS.</summary>
    public static bool IsJws(string? qrCode) => qrCode is not null && qrCode.Split('.').Length == 3;

    /// <summary>Takes a compact JWS apart; the payload decoded to its UTF-8 string.</summary>
    /// <exception cref="FormatException">Not three segments, a segment that is not Base64Url, or a header that is not a JSON object with <c>alg</c>.</exception>
    public static Parts Decode(string jws)
    {
        var segments = jws.Split('.');
        if (segments.Length != 3)
        {
            throw new FormatException("A compact JWS has three segments.");
        }
        using var header = JsonDocument.Parse(FromBase64Url(segments[0]));
        if (header.RootElement.ValueKind != JsonValueKind.Object || !header.RootElement.TryGetProperty("alg", out var alg) || alg.ValueKind != JsonValueKind.String)
        {
            throw new FormatException("The JWS header is no JSON object with alg.");
        }
        var kid = header.RootElement.TryGetProperty("kid", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
        return new Parts(segments[0], segments[1], segments[2], alg.GetString()!, string.IsNullOrEmpty(kid) ? null : kid, Encoding.UTF8.GetString(FromBase64Url(segments[1])));
    }

    /// <summary>Verifies an ES256 JWS against <paramref name="publicKey"/>.</summary>
    public static bool VerifyES256(string jws, ECDsa publicKey)
    {
        var parts = Decode(jws);
        return parts.Algorithm == ES256
            && publicKey.VerifyData(Encoding.ASCII.GetBytes($"{parts.Header}.{parts.Payload}"), FromBase64Url(parts.Signature), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    /// <summary>RFC 4648 §5 Base64Url, padded or not.</summary>
    /// <exception cref="FormatException">Not Base64Url.</exception>
    public static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '='));
    }

    /// <summary>The segments of a compact JWS, with the header's <c>alg</c> and <c>kid</c> and the decoded payload.</summary>
    public sealed record Parts(string Header, string Payload, string Signature, string Algorithm, string? KeyId, string SignedString);
}
