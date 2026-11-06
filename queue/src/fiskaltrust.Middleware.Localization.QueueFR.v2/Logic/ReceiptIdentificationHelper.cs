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
}
