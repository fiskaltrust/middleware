using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;

namespace fiskaltrust.Middleware.Localization.QueuePT.Helpers;

public static class ReceiptIdentificationHelper
{
    /// <summary>
    /// Generates the receipt identification by appending the series identifier and formatted numerator to the existing ftReceiptIdentification.
    /// The numerator is formatted as a 4-digit zero-padded string.
    /// </summary>
    /// <param name="receiptResponse">The receipt response to update</param>
    /// <param name="series">The number series containing the identifier and numerator</param>
    public static void AppendSeriesIdentification(ReceiptResponse receiptResponse, NumberSeries series)
    {
        receiptResponse.ftReceiptIdentification += series.Identifier + "/" + series.Numerator!.ToString();
    }
}