using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Storage.Base.Helpers;

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
    private readonly FRLateSigning _lateSigning;

    public FRSigningPipeline(IFRSSCD sscd, FRChainStateProvider chains, AsyncLazy<IMiddlewareQueueItemRepository>? queueItems = null)
    {
        _sscd = sscd;
        _chains = chains;
        _lateSigning = new FRLateSigning(queueItems);
    }

    public Task<ProcessCommandResponse> SignAsync(ProcessCommandRequest request, List<ftActionJournal>? actionJournals = null)
        => _chains.ExecuteInChainAsync(request.ReceiptRequest, async (chain, totals, lateSigning) =>
        {
            var receiptResponse = request.ReceiptResponse;
            var identificationBeforeSigning = receiptResponse.ftReceiptIdentification;

            // A closing attests the turnover accumulated up to it; the closing request itself
            // carries no items, so the snapshot is taken before the receipt is added. A training
            // closing prints the same live figures but closes nothing (PeriodsClosedBy is empty).
            var closingPeriod = FRPeriodTotalsAccumulator.ReportedPeriodOf(request.ReceiptRequest);
            var periodTotals = closingPeriod is null ? null : totals.Snapshot(closingPeriod.Value);

            chain.Numerator++;
            ReceiptIdentificationHelper.AppendChainIdentification(receiptResponse, chain);
            FRTypeOfServiceCalculator.Apply(request.ReceiptRequest, receiptResponse);
            FRArchiveStateData.Apply(request.ReceiptRequest, receiptResponse, chain.Chain, periodTotals);

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

            chain.LastSignature = signature;
            totals.Add(request.ReceiptRequest);

            // A closing also ends the shorter periods (v1.2 cascade, FRPeriodTotalsAccumulator),
            // and records in the 1.2 columns which receipt closed each of them.
            foreach (var period in FRPeriodTotalsAccumulator.PeriodsClosedBy(request.ReceiptRequest))
            {
                totals.Close(period, receiptResponse.ftReceiptMoment, receiptResponse.ftQueueItemID);
            }

            // Late signing follows the signature: only a signed entry opens or closes the range.
            var lateSigningJournals = await _lateSigning.ApplySignedAsync(request.ReceiptRequest, response.ReceiptResponse, lateSigning).ConfigureAwait(false);

            return new ProcessCommandResponse(response.ReceiptResponse, [.. actionJournals ?? [], .. lateSigningJournals]);

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
