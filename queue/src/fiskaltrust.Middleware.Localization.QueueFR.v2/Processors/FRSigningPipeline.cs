using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
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

    /// <param name="recordSigned">
    /// Records what the signed entry depends on (the copy journal of a duplicate), inside the chain
    /// lock and before the chain advances. If it fails the entry is not issued: the number is not
    /// consumed and is signed again on the retry, so a record never lags behind the chain.
    /// </param>
    public Task<ProcessCommandResponse> SignAsync(ProcessCommandRequest request, List<ftActionJournal>? actionJournals = null, int? copyNumber = null, Func<ReceiptResponse, Task>? recordSigned = null)
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
            FRTypeOfServiceCalculator.Apply(request.ReceiptRequest, receiptResponse);

            // Every document of the training chain carries the simulation mention, whatever its
            // receipt case: this is the one path all of them take.
            if (chain.Chain == FRReceiptChain.Training)
            {
                receiptResponse.AddSignatureItem(SignaturItemFactory.CreateTrainingSignature());
            }

            ProcessResponse response;
            string signature;
            try
            {
                (response, signature) = await _sscd.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request.ReceiptRequest,
                    ReceiptResponse = receiptResponse,
                    PeriodTotals = periodTotals,
                    ChainCounter = chain.Numerator,
                    CopyNumber = copyNumber,
                }, chain.LastSignature).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // The SCU reports its own failures in the response; an exception means no answer
                // came back at all - the SCU unreachable, the transport failing.
                return Unsigned(ex.Message, refused: false);
            }

            if (FRSSCDErrorHandling.IsError(response.ReceiptResponse))
            {
                return Unsigned(FRSSCDErrorHandling.Reason(response.ReceiptResponse), FRSSCDErrorHandling.IsRefusal(response.ReceiptResponse));
            }

            if (recordSigned is not null)
            {
                try
                {
                    await recordSigned(response.ReceiptResponse).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    chain.Numerator--;
                    receiptResponse.ftReceiptIdentification = identificationBeforeSigning;
                    receiptResponse.SetReceiptResponseError($"The receipt was signed but could not be recorded, so it was not issued; retry it. Details: {ex.Message}");
                    return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>());
                }
            }

            chain.LastSignature = signature;
            totals.Add(request.ReceiptRequest);

            // A closing also ends the shorter periods (v1.2 cascade, FRPeriodTotalsAccumulator).
            foreach (var period in FRPeriodTotalsAccumulator.PeriodsClosedBy(request.ReceiptRequest))
            {
                totals.Reset(period);
            }

            return new ProcessCommandResponse(response.ReceiptResponse, actionJournals ?? new List<ftActionJournal>());

            // No document was issued, so neither the number nor the period may move - NF525
            // requires the national numbering of every chain to be gapless, and an unsigned
            // closing must leave the period open so it can be retried.
            ProcessCommandResponse Unsigned(string reason, bool refused)
            {
                chain.Numerator--;
                receiptResponse.ftReceiptIdentification = identificationBeforeSigning;
                if (refused)
                {
                    receiptResponse.SetRefusedError(reason);
                }
                else
                {
                    receiptResponse.SetSigningUnavailableError(reason);
                }

                return new ProcessCommandResponse(receiptResponse, new List<ftActionJournal>());
            }
        });

    /// <summary>True if the receipt came back signed, i.e. the chain actually advanced.</summary>
    public static bool Succeeded(ProcessCommandResponse response) => !response.receiptResponse.ftState.IsState(State.Error);
}
