using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// The single path through which a French receipt becomes a signed, chained entry: take the next
/// number of its chain, let the SCU sign it against the chain's previous hash, and advance the
/// chain and the period totals only once the signature exists.
/// </summary>
public class FRSigningPipeline
{
    private readonly IFRSSCD _sscd;
    private readonly FRChainStateProvider _chains;

    public FRSigningPipeline(IFRSSCD sscd, FRChainStateProvider chains)
    {
        _sscd = sscd;
        _chains = chains;
    }

    public Task<ProcessCommandResponse> SignAsync(ProcessCommandRequest request, List<ftActionJournal>? actionJournals = null)
        => _chains.ExecuteInChainAsync(request.ReceiptRequest, async (chain, totals) =>
        {
            var receiptResponse = request.ReceiptResponse;
            var identificationBeforeSigning = receiptResponse.ftReceiptIdentification;

            // A closing attests the turnover accumulated up to it; the closing request itself
            // carries no items, so the snapshot is taken before the receipt is added.
            var closingPeriod = FRPeriodTotalsAccumulator.PeriodOf(request.ReceiptRequest);
            var periodTotals = closingPeriod is null ? null : totals.Snapshot(closingPeriod.Value);

            chain.Numerator++;
            ReceiptIdentificationHelper.AppendChainIdentification(receiptResponse, chain);

            try
            {
                var (response, hash) = await _sscd.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request.ReceiptRequest,
                    ReceiptResponse = receiptResponse,
                    PeriodTotals = periodTotals,
                }, chain.LastHash).ConfigureAwait(false);

                chain.LastHash = hash;
                totals.Add(request.ReceiptRequest);

                if (closingPeriod is not null)
                {
                    totals.Reset(closingPeriod.Value);
                }

                return new ProcessCommandResponse(response.ReceiptResponse, actionJournals ?? new List<ftActionJournal>());
            }
            catch (Exception ex) when (FRSSCDErrorHandling.IsSigningUnavailable(ex))
            {
                // No document was issued, so neither the number nor the period may move - NF525
                // requires the national numbering of every chain to be gapless, and an unsigned
                // closing must leave the period open so it can be retried.
                chain.Numerator--;
                receiptResponse.ftReceiptIdentification = identificationBeforeSigning;
                receiptResponse.SetSigningUnavailableError(ex);
                return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>());
            }
        });

    /// <summary>True if the receipt came back signed, i.e. the chain actually advanced.</summary>
    public static bool Succeeded(ProcessCommandResponse response) => !response.receiptResponse.ftState.IsState(State.Error);
}
