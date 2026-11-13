using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;

/// <summary>
/// Holds a v1.2 queue that has not been migrated to the two migration receipts (market-fr ADR 0003):
/// any other request is answered with an error and nothing is signed, so no v2 entry is chained onto
/// v1 state that was not handed over.
/// </summary>
public static class FRMigrationGate
{
    public const ReceiptCase InitiateMigration = (ReceiptCase) 0x4021;
    public const ReceiptCase FinishMigration = (ReceiptCase) 0x4022;

    public static Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)>> Wrap(
        FRChainStateProvider chains,
        Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)>> process)
        => async (request, response, queue, queueItem) =>
        {
            if (request.ftReceiptCase.Case() is not (InitiateMigration or FinishMigration)
                && await chains.ReadAsync(FRV1Handover.AwaitsMigration).ConfigureAwait(false))
            {
                response.SetReceiptResponseError("This v1.2 queue has not been migrated to v2 yet. It accepts only the migration receipts: initiate migration (0x4021), then finish migration (0x4022).");
                return (response, new List<ftActionJournal>());
            }

            return await process(request, response, queue, queueItem).ConfigureAwait(false);
        };
}
