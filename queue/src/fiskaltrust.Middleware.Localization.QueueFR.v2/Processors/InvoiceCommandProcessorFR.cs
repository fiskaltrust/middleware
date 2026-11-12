using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

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

    public Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => SignInvoice(request);

    public Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => SignInvoice(request);

    public Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => SignInvoice(request);

    public Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => SignInvoice(request);

    /// <summary>
    /// An invoice prints its sales-line count as a ticket does (R12 <c>FAC-LIG-NBR</c>, archive-v2 §1.2 <c>LineCount</c> on
    /// chain I). The LNE data set already signs the count, so nothing signed changes.
    /// </summary>
    private Task<ProcessCommandResponse> SignInvoice(ProcessCommandRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateSalesLineCountSignature(FRSalesLines.Count(request.ReceiptRequest)));
        return _pipeline.SignAsync(request);
    }
}
