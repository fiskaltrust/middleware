using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// The closings are the French "grand total" receipts: NF525 requires a signed and chained
/// cumulative total per period, so every closing is signed into the closing chain of its own type
/// (ADR 0002) rather than being a queue-side bookkeeping no-op. The zero receipt and the other
/// non-closing daily operations go to the technical chain.
/// </summary>
public class DailyOperationsCommandProcessorFR : IDailyOperationsCommandProcessor
{
    private readonly FRSigningPipeline _pipeline;

    public DailyOperationsCommandProcessorFR(FRSigningPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    /// <summary>The zero receipt proves the chain is intact without moving any total.</summary>
    public Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request) => ClosingAsync(request, "Shift-Closing receipt was processed.");

    public Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request) => ClosingAsync(request, "Daily-Closing receipt was processed.");

    public Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request) => ClosingAsync(request, "Monthly-Closing receipt was processed.");

    public Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request) => ClosingAsync(request, "Yearly-Closing receipt was processed.");

    /// <summary>
    /// Signs a closing, then journals it ([03 F13]): dated by the closing receipt's moment and naming it, with the period it
    /// closed and its totals as stored in the response, as v1.2 journalled its grand-total payload. An unsigned closing journals nothing.
    /// </summary>
    private async Task<ProcessCommandResponse> ClosingAsync(ProcessCommandRequest request, string message)
    {
        var signed = await _pipeline.SignAsync(request).ConfigureAwait(false);
        if (!FRSigningPipeline.Succeeded(signed))
        {
            return signed;
        }

        return signed with { actionJournals = [ftActionJournalFactory.CreateClosingActionJournal(request.ReceiptRequest, signed.receiptResponse, message), .. signed.actionJournals] };
    }
}
