using System.Globalization;
using fiskaltrust.ifPOS.v2;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// The French receipt identification is the chain letter followed by that chain's national
/// up-counting number, e.g. <c>T42</c> for the 42nd ticket.
/// </summary>
public static class ReceiptIdentificationHelper
{
    public static void AppendChainIdentification(ReceiptResponse receiptResponse, FRChainState chain)
        => receiptResponse.ftReceiptIdentification += $"{chain.Identifier}{chain.Numerator.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Reads the number back out of an identification. Returns null when the identification does
    /// not belong to the given chain, so a caller can keep walking the queue items.
    /// </summary>
    public static long? ReadNumerator(string? receiptIdentification, string chainIdentifier)
    {
        if (string.IsNullOrEmpty(receiptIdentification))
        {
            return null;
        }

        var index = receiptIdentification.LastIndexOf(chainIdentifier, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        var digits = receiptIdentification[(index + chainIdentifier.Length)..];
        return long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var numerator) ? numerator : null;
    }
}
