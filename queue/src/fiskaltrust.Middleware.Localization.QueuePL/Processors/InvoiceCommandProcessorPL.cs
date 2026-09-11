using fiskaltrust.Middleware.Localization.QueuePL.Factories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueuePL.Processors;

/// <summary>
/// Invoice cases are deliberately NOT sent to the register — invoices are fiscalized via KSeF,
/// not via the cash register. Until an SCU.PL.KSeF is configured the request/response pair is
/// persisted as a queue item (done by the shared SignProcessor) and marked with an informational
/// "stored, not fiscalized" signature. Switching to a KSeF processor later is a configuration
/// change, not a breaking one.
/// </summary>
public class InvoiceCommandProcessorPL : IInvoiceCommandProcessor
{
    public Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateStoredNotFiscalizedSignature());
        return Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));
    }

    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => InvoiceUnknown0x1000Async(request);

    public Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => InvoiceUnknown0x1000Async(request);

    public Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => InvoiceUnknown0x1000Async(request);
}
