using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// Signs the sales-side receipt cases as QueueFR v1.2 did through the PosSystem API v2→v0
/// translation (market-fr#221). Tickets, e-commerce sales and sales flagged as made in a foreign
/// country go into the ticket chain and the grand totals. A sale without fiscalization obligation
/// (a v1.2 foreign sale) and the provisional documents (table check, pro forma) are signed in the bill chain, outside
/// the grand totals; the provisional ones carry the mention that they are not a receipt. A
/// delivery note is neither signed nor numbered.
/// </summary>
public class ReceiptCommandProcessorFR : IReceiptCommandProcessor
{
    private readonly FRSigningPipeline _pipeline;

    public ReceiptCommandProcessorFR(FRSigningPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => PointOfSaleReceipt0x0001Async(request);

    public Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request) => SignTicket(request);

    /// <summary>The payment proof (justificatif de paiement) has its own chain, numbered with "P".</summary>
    public Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreatePaymentProofSignature());
        return _pipeline.SignAsync(request);
    }

    /// <summary>
    /// v1.2 signs it as a foreign sale (v0 0x0E) in the bill chain. It is still a sale, printed as a
    /// ticket (market-fr#221).
    /// </summary>
    public Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => SignTicket(request);

    /// <summary>An e-commerce sale is a ticket in the ticket chain and the grand totals (market-fr#221).</summary>
    public Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => SignTicket(request);

    /// <summary>v1.2 neither signs nor numbers a delivery note (v0 0x09).</summary>
    public Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> TableCheck0x0006Async(ProcessCommandRequest request) => SignProvisionalDocument(request);

    public Task<ProcessCommandResponse> ProForma0x0007Async(ProcessCommandRequest request) => SignProvisionalDocument(request);

    /// <summary>
    /// The sales (0x0001, 0x0003, 0x0004): the chain (ticket or bill) comes from the receipt case,
    /// and each prints its sales-line count (R13 <c>ENC-TIK-LIG-NBR</c>). The payment proof does
    /// not, so <see cref="PaymentTransfer0x0002Async"/> signs without it.
    /// </summary>
    private Task<ProcessCommandResponse> SignTicket(ProcessCommandRequest request)
    {
        AddSalesLineCount(request);
        return _pipeline.SignAsync(request);
    }

    /// <summary>A provisional document is a note in R13: it prints the mention and the sales-line count.</summary>
    private Task<ProcessCommandResponse> SignProvisionalDocument(ProcessCommandRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateProvisionalDocumentSignature());
        AddSalesLineCount(request);
        return _pipeline.SignAsync(request);
    }

    private static void AddSalesLineCount(ProcessCommandRequest request)
        => request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateSalesLineCountSignature(FRSalesLines.Count(request.ReceiptRequest)));
}
