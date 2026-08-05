using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.Localization.QueuePL.Factories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePL.Processors;

/// <summary>
/// Daily operations map to register operations: the zero receipt reads the device status, the
/// daily closing triggers the legally required daily (Z) report, monthly/yearly closings trigger
/// the periodic report. All of them go through the SCU — the register owns the report counters.
/// </summary>
public class DailyOperationsCommandProcessorPL(IPLSSCD sscd) : IDailyOperationsCommandProcessor
{
    private readonly IPLSSCD _sscd = sscd;

    public async Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request) => await SubmitAsync(request);

    public async Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request)
    {
        var response = await SubmitAsync(request);
        return new ProcessCommandResponse(response.receiptResponse, [ftActionJournalFactory.CreateDailyClosingActionJournal(request.queue, request.ReceiptRequest, request.ReceiptResponse)]);
    }

    public async Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request)
    {
        var response = await SubmitAsync(request);
        return new ProcessCommandResponse(response.receiptResponse, [ftActionJournalFactory.CreateMonthlyClosingActionJournal(request.queue, request.ReceiptRequest, request.ReceiptResponse)]);
    }

    public async Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request) => await SubmitAsync(request);

    private async Task<ProcessCommandResponse> SubmitAsync(ProcessCommandRequest request)
    {
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        });
        return new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>());
    }
}
