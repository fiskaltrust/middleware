using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Carries the per-rate VAT summary an SCU computed (Infocert R13 v4.2 <i>Récapitulatif</i>) into the
/// <c>FR</c> block of the response's <c>ftStateData</c>, from where it reaches the journal and the
/// archive (fiskaltrust/market-fr#147). The queue does not compute it: only the InfoCert SCU produces
/// the summary (ADR 0004), so a response signed by another SCU is left untouched.
/// </summary>
public static class FRVatSummary
{
    public static void Apply(ReceiptResponse response, Dictionary<string, FRVatRateSummary>? summary)
    {
        if (summary is null)
        {
            return;
        }

        var stateData = MiddlewareStateData.FromReceiptResponse(response);
        stateData.FR ??= new MiddlewareStateDataFR();
        stateData.FR.VATSummary = summary;
        response.ftStateData = stateData;
    }
}
