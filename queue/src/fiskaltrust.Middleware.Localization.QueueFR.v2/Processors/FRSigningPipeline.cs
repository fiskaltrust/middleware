using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// The single path through which a French receipt becomes a signed, chained entry: take the next
/// number of its chain, let the SCU sign it against the chain's previous signature, and advance the
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

    /// <summary>The fiscal state the pipeline signs against, e.g. whether an SCU switch is pending.</summary>
    public FRChainStateProvider Chains => _chains;

    public Task<ProcessCommandResponse> SignAsync(ProcessCommandRequest request, List<ftActionJournal>? actionJournals = null)
        => _chains.ExecuteInChainAsync(request.ReceiptRequest, async (chain, totals) =>
        {
            var receiptResponse = request.ReceiptResponse;

            // ADR 0005: between 0x4011 and 0x4012 the outgoing SCU has signed its last entry and the
            // incoming one is not confirmed yet, so nothing else may enter any chain (market-fr#177).
            if (_chains.PendingScuSwitch is { } pending && request.ReceiptRequest.ftReceiptCase.Case() != ReceiptCase.FinishSCUSwitch0x4012)
            {
                receiptResponse.SetReceiptResponseError($"An SCU switch is pending: the outgoing SCU ({pending.Outgoing}) signed the init-SCU-switch receipt (0x4011). Configure the incoming SCU and send the finish-SCU-switch receipt (0x4012) before any other receipt.");
                return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>());
            }

            var identificationBeforeSigning = receiptResponse.ftReceiptIdentification;

            // A closing attests the turnover accumulated up to it; the closing request itself
            // carries no items, so the snapshot is taken before the receipt is added.
            var closingPeriod = FRPeriodTotalsAccumulator.PeriodOf(request.ReceiptRequest);
            var periodTotals = closingPeriod is null ? null : totals.Snapshot(closingPeriod.Value);

            chain.Numerator++;
            ReceiptIdentificationHelper.AppendChainIdentification(receiptResponse, chain);
            FRTypeOfServiceCalculator.Apply(request.ReceiptRequest, receiptResponse);

            try
            {
                var (response, signature) = await _sscd.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request.ReceiptRequest,
                    ReceiptResponse = receiptResponse,
                    PeriodTotals = periodTotals,
                    ChainCounter = chain.Numerator,
                }, chain.LastSignature).ConfigureAwait(false);

                chain.LastSignature = signature;
                totals.Add(request.ReceiptRequest);
                _chains.TrackScuSwitch(request.ReceiptRequest, response.ReceiptResponse);

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
