using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2;

public class InvoiceCommandProcessorIT : IInvoiceCommandProcessor
{
    public Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));
}
