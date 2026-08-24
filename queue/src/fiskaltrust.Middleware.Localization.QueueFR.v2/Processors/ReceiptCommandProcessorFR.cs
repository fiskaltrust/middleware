using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// Signs the sales-side receipt cases. Everything handed to the customer is signed and chained -
/// NF525 makes no difference between a cash sale and an e-commerce sale - while the provisional
/// documents (bill, pro forma, delivery note) go into their own chain and carry the mention that
/// they are not a receipt.
/// </summary>
public class ReceiptCommandProcessorFR : IReceiptCommandProcessor
{
    private readonly FRSigningPipeline _pipeline;

    public ReceiptCommandProcessorFR(FRSigningPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => PointOfSaleReceipt0x0001Async(request);

    public Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    /// <summary>The payment proof (justificatif de paiement) has its own chain, numbered with "P".</summary>
    public Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    /// <summary>
    /// A sale explicitly issued without a fiscalization obligation is stored but not signed, so it
    /// cannot consume a number of a fiscal chain.
    /// </summary>
    public Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateStoredNotSignedSignature());
        return FRFallBackOperations.NoOp(request);
    }

    public Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => SignProvisionalDocument(request);

    public Task<ProcessCommandResponse> TableCheck0x0006Async(ProcessCommandRequest request) => SignProvisionalDocument(request);

    public Task<ProcessCommandResponse> ProForma0x0007Async(ProcessCommandRequest request) => SignProvisionalDocument(request);

    private Task<ProcessCommandResponse> SignProvisionalDocument(ProcessCommandRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateProvisionalDocumentSignature());
        return _pipeline.SignAsync(request);
    }
}
