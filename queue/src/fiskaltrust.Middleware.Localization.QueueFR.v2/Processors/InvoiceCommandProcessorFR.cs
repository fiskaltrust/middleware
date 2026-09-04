using fiskaltrust.Middleware.Localization.v2;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// Invoices are signed and chained like receipts, but in their own chain numbered with "I".
/// The B2C/B2B/B2G distinction does not change the French signature - it only shows up in the
/// customer data the invoice carries - so all four cases share one path.
/// </summary>
public class InvoiceCommandProcessorFR : IInvoiceCommandProcessor
{
    private readonly FRSigningPipeline _pipeline;

    public InvoiceCommandProcessorFR(FRSigningPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);
}
