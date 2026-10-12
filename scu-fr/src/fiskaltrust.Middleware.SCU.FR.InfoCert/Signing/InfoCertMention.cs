using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;

/// <summary>
/// The NF525 mention every justificatif carries (NF525 V2.3 §6.1.6.1, market-fr#131), e.g.
/// <c>(NF525) B 0546 QpwW OnQPMS 06.07</c>: the mark, the software category, the product number on
/// the NF525 certificate, the characters at positions 3, 7, 13 and 19 of the signature, the optional
/// software name and the software version, separated by single spaces.
/// </summary>
public static class InfoCertMention
{
    public const string Mark = "(NF525)";

    /// <summary>The 1-based positions of the signature that §6.1.6.1 prints.</summary>
    public static readonly int[] ExtractPositions = { 3, 7, 13, 19 };

    public static readonly string[] Categories = { "A", "B", "C" };

    /// <summary>
    /// Builds the mention, or returns null when the category, the product number, the version or a
    /// long enough signature is missing: an incomplete mention is never emitted.
    /// </summary>
    public static string? Build(string? category, string? productNumber, string signature, string? softwareName, string? softwareVersion)
    {
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(productNumber) || string.IsNullOrWhiteSpace(softwareVersion))
        {
            return null;
        }

        var extract = SignatureExtract(signature);
        if (extract is null)
        {
            return null;
        }

        var parts = new List<string> { Mark, category!, productNumber!, extract };
        if (!string.IsNullOrWhiteSpace(softwareName))
        {
            parts.Add(softwareName!);
        }

        parts.Add(softwareVersion!);
        return string.Join(" ", parts);
    }

    public static string? SignatureExtract(string signature)
    {
        if (signature.Length < ExtractPositions[^1])
        {
            return null;
        }

        var chars = new char[ExtractPositions.Length];
        for (var i = 0; i < ExtractPositions.Length; i++)
        {
            chars[i] = signature[ExtractPositions[i] - 1];
        }

        return new string(chars);
    }
}
