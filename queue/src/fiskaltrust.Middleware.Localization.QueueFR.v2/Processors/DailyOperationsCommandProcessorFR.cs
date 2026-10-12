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

    public Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request)
        => _pipeline.SignAsync(request, [ftActionJournalFactory.CreateDailyClosingActionJournal(request.queue, request.ReceiptRequest, request.ReceiptResponse)]);

    public Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request)
        => _pipeline.SignAsync(request, [ftActionJournalFactory.CreateMonthlyClosingActionJournal(request.queue, request.ReceiptRequest, request.ReceiptResponse)]);

    public Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request)
        => _pipeline.SignAsync(request, [ftActionJournalFactory.CreateYearlyClosingActionJournal(request.queue, request.ReceiptRequest, request.ReceiptResponse)]);
}
