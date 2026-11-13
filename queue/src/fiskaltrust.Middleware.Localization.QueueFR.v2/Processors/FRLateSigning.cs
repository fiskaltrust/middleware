using System.Globalization;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Storage.Base.Helpers;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// Late signing (market-fr#231). A POS that could not reach the middleware issues its receipts as
/// "electronic recording system failed" and, once the middleware is reachable again, re-sends them
/// with <see cref="ReceiptCaseFlags.LateSigning"/>. They are signed normally, in arrival order, into
/// the chain of their receipt case: the flag is part of the signed <c>ftReceiptCase</c>, so every late-signed entry
/// is marked inside its own signature. On top of that the queue
/// <list type="bullet">
/// <item>keeps the range open (<see cref="FRLateSigningState"/>) from the first signed late-signed
/// receipt to the next signed zero receipt, and sets <see cref="StateFlags.LateSigningModeIsActive"/>
/// on every response in between;</item>
/// <item>asks the POS for the JET event 70 "Début de mode dégradé" (R19 v4.2) through a JET
/// signature item: the queue issues no JET entry itself, the POS sends the event as a
/// <c>ProtocolTechnicalEvent 0x3001</c> (decision of 2026-11-04 on market-fr#231), and for JET 120
/// "Fin de mode dégradé" on the zero receipt that closes the range (contract §6, ADR 0009);</item>
/// <item>prints the range on the closing zero receipt and journals the start and end of the mode,
/// as v1.2 did.</item>
/// </list>
/// Training receipts neither open nor close the mode: the training chain stays apart from the
/// fiscal chains.
/// </summary>
public class FRLateSigning
{

    private readonly AsyncLazy<IMiddlewareQueueItemRepository>? _queueItems;

    /// <param name="queueItems">
    /// Where the first late-signed receipt is read back when the mode closes. Without it the range
    /// names that receipt by its queue item id.
    /// </param>
    public FRLateSigning(AsyncLazy<IMiddlewareQueueItemRepository>? queueItems = null)
    {
        _queueItems = queueItems;
    }

    /// <summary>
    /// Folds a signed receipt into the late-signing range, adds the items the response has to carry
    /// and returns the action journal entries to write.
    /// </summary>
    public async Task<List<ftActionJournal>> ApplySignedAsync(ReceiptRequest request, ReceiptResponse response, FRLateSigningState state)
    {
        var journals = new List<ftActionJournal>();
        var training = request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Training);

        if (!training && request.ftReceiptCase.IsFlag(ReceiptCaseFlags.LateSigning) && state.Add(response.ftQueueItemID, request.cbReceiptMoment))
        {
            var information = FRJetItems.Information(
                ("cbReceiptReference", request.cbReceiptReference),
                ("ftReceiptIdentification", response.ftReceiptIdentification),
                ("cbReceiptMoment", FRJetItems.Moment(request.cbReceiptMoment)));
            response.ftSignatures.Add(SignaturItemFactory.CreateJetEventSignature(FRJetItems.DegradedModeStart, FRJetItems.DegradedModeStartLabel, information));
            journals.Add(ftActionJournalFactory.CreateLateSigningStartedActionJournal(request, response));
        }

        if (!training && request.ftReceiptCase.Case() == ReceiptCase.ZeroReceipt0x2000 && state.IsOpen)
        {
            var range = state.Close();
            var (firstReference, firstIdentification) = await FirstReceiptAsync(range, request, response).ConfigureAwait(false);
            response.ftSignatures.Add(SignaturItemFactory.CreateLateSigningEndedSignature(firstIdentification, response.ftReceiptIdentification));
            response.ftSignatures.Add(SignaturItemFactory.CreateJetEventSignature(FRJetItems.DegradedModeEnd, FRJetItems.DegradedModeEndLabel, FRJetItems.Information(
                ("count", range.Count.ToString(CultureInfo.InvariantCulture)),
                ("first.cbReceiptReference", firstReference),
                ("first.ftReceiptIdentification", firstIdentification),
                ("cbReceiptMomentMin", FRJetItems.Moment(range.MomentMin)),
                ("cbReceiptMomentMax", FRJetItems.Moment(range.MomentMax)),
                ("closing.cbReceiptReference", request.cbReceiptReference),
                ("closing.ftReceiptIdentification", response.ftReceiptIdentification))));
            journals.Add(ftActionJournalFactory.CreateLateSigningEndedActionJournal(request, response, range, firstReference, firstIdentification));
        }

        MarkMode(response, state);
        return journals;
    }

    /// <summary>Sets <see cref="StateFlags.LateSigningModeIsActive"/> while the mode is open.</summary>
    public static void MarkMode(ReceiptResponse response, FRLateSigningState state)
    {
        if (state.IsOpen)
        {
            response.ftState = response.ftState.WithFlag(StateFlags.LateSigningModeIsActive);
        }
    }

    private async Task<(string? reference, string? identification)> FirstReceiptAsync(FRLateSigningRange range, ReceiptRequest request, ReceiptResponse response)
    {
        if (range.FirstQueueItemId is not { } firstId)
        {
            return (null, null);
        }

        // A late-signed zero receipt opens and closes the mode itself.
        if (firstId == response.ftQueueItemID)
        {
            return (request.cbReceiptReference, response.ftReceiptIdentification);
        }

        if (_queueItems is not null)
        {
            try
            {
                var queueItem = await (await _queueItems).GetAsync(firstId).ConfigureAwait(false);
                if (queueItem is not null)
                {
                    var firstRequest = string.IsNullOrEmpty(queueItem.request) ? null : JsonSerializer.Deserialize<ReceiptRequest>(queueItem.request);
                    var firstResponse = string.IsNullOrEmpty(queueItem.response) ? null : JsonSerializer.Deserialize<ReceiptResponse>(queueItem.response);
                    return (firstRequest?.cbReceiptReference, firstResponse?.ftReceiptIdentification ?? firstId.ToString());
                }
            }
            catch (Exception)
            {
                // The receipt is already signed and the range is closed; the mention falls back to
                // the queue item id rather than losing the signature to a storage read.
            }
        }

        return (null, firstId.ToString());
    }
}
