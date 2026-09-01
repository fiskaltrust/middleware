using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.be;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueBE.Factories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueBE.Processors;

public class DailyOperationsCommandProcessorBE(IBESSCD sscd) : IDailyOperationsCommandProcessor
{
    private readonly IBESSCD _sscd = sscd;

    /// <summary>
    /// The zero receipt carries no fiscal content; it is handed to the SCU so that whatever
    /// the SCU decides a BE zero receipt means stays in one place. Today the ZwarteDoos SCU
    /// answers it locally without contacting the FDM.
    /// </summary>
    public async Task<ProcessCommandResponse> ZeroReceipt0x2000Async(ProcessCommandRequest request)
    {
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse
        });
        return new ProcessCommandResponse(response.ReceiptResponse, []);
    }

    public async Task<ProcessCommandResponse> OneReceipt0x2001Async(ProcessCommandRequest request) => await FallBackOperations.NotYetImplemented(request);

    public async Task<ProcessCommandResponse> ShiftClosing0x2010Async(ProcessCommandRequest request) => await FallBackOperations.NotYetImplemented(request);

    /// <summary>
    /// The daily closing is the Belgian Z report: the SCU turns it into a REPORT_TURNOVER_Z
    /// event on the FDM and returns its signature. The action journal entry is only written when
    /// the FDM actually signed — neither an errored response nor an unsigned one may look like a
    /// closed day.
    /// </summary>
    public async Task<ProcessCommandResponse> DailyClosing0x2011Async(ProcessCommandRequest request)
    {
        var (queue, receiptRequest, receiptResponse) = request;
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = receiptRequest,
            ReceiptResponse = receiptResponse
        });

        if (response.ReceiptResponse.ftState.IsState(State.Error))
        {
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }

        // Defence in depth, and the reason this is checked here rather than trusted from the SCU:
        // the SCU is what talks to the FDM, but the QUEUE is what records the day as closed. A
        // signed Z report always comes back carrying the FDM's signature, so a response with no
        // signature at all closed nothing — however clean its state looks.
        if (response.ReceiptResponse.ftSignatures.Count == 0)
        {
            response.ReceiptResponse.SetReceiptResponseError("The daily closing was not signed: the SCU returned no signature for the Z report.");
            return new ProcessCommandResponse(response.ReceiptResponse, []);
        }

        var actionJournal = ftActionJournalFactory.CreateDailyClosingActionJournal(queue, receiptRequest, response.ReceiptResponse);
        return new ProcessCommandResponse(response.ReceiptResponse, [actionJournal]);
    }

    /// <remarks>
    /// Deliberately still unimplemented: the ZwarteDoos SCU has no monthly/yearly closing
    /// branch, so routing these here would return a clean state for a periodic closing that
    /// never reached the FDM. Failing loudly is the honest answer until the FDM report type
    /// for those periods is decided.
    /// </remarks>
    public async Task<ProcessCommandResponse> MonthlyClosing0x2012Async(ProcessCommandRequest request) => await FallBackOperations.NotYetImplemented(request);

    /// <inheritdoc cref="MonthlyClosing0x2012Async"/>
    public async Task<ProcessCommandResponse> YearlyClosing0x2013Async(ProcessCommandRequest request) => await FallBackOperations.NotYetImplemented(request);
}
