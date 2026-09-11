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

    public async Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request) => (await SubmitAsync(request)).Response;

    public async Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request) => await PLFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request)
    {
        var (response, reportWasPrinted) = await SubmitAsync(request);
        return reportWasPrinted
            ? new ProcessCommandResponse(response.receiptResponse, [ftActionJournalFactory.CreateDailyClosingActionJournal(request.queue, request.ReceiptRequest, request.ReceiptResponse)])
            : response;
    }

    public async Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request)
    {
        var (response, reportWasPrinted) = await SubmitAsync(request);
        return reportWasPrinted
            ? new ProcessCommandResponse(response.receiptResponse, [ftActionJournalFactory.CreateMonthlyClosingActionJournal(request.queue, request.ReceiptRequest, request.ReceiptResponse)])
            : response;
    }

    public async Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request) => (await SubmitAsync(request)).Response;

    /// <summary>
    /// Hands the receipt to the register and reports whether it got there. The action journal of a
    /// closing is the audit record that the report was printed and the counters advanced — writing
    /// it after an unreachable register would claim a Z report that never ran.
    /// </summary>
    private async Task<(ProcessCommandResponse Response, bool Submitted)> SubmitAsync(ProcessCommandRequest request)
    {
        try
        {
            var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
            {
                ReceiptRequest = request.ReceiptRequest,
                ReceiptResponse = request.ReceiptResponse,
            });
            return (new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>()), true);
        }
        catch (Exception ex) when (PLSSCDErrorHandling.IsDeviceUnreachable(ex))
        {
            request.ReceiptResponse.SetDeviceUnreachableError(ex);
            return (new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()), false);
        }
    }
}
