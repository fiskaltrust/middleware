using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueIT.Processors;

/// <summary>
/// Invoices are stored by the queue but not handed to the RT device; the "fattura" is issued outside of the middleware.
/// </summary>
public class InvoiceCommandProcessorIT : IInvoiceCommandProcessor
{
    public async Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => await ITFallBackOperations.NoOp(request);
}
