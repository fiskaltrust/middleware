using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;

/// <summary>
/// The log cases feed the French technical event and accounting log ("journal des evenements"),
/// which NF525 requires to be signed and chained just like the sales chains - chain "L". Copies of
/// an already issued document go into the duplicate chain ("D") and must be marked as a duplicata.
/// </summary>
public class ProtocolCommandProcessorFR : IProtocolCommandProcessor
{
    private readonly FRSigningPipeline _pipeline;

    public ProtocolCommandProcessorFR(FRSigningPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => _pipeline.SignAsync(request);

    /// <summary>Orders are internal working steps and never leave the till as a document.</summary>
    public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request) => FRFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request)
    {
        request.ReceiptResponse.AddSignatureItem(SignaturItemFactory.CreateDuplicateSignature(request.ReceiptRequest.cbPreviousReceiptReference?.ToString()));
        return _pipeline.SignAsync(request);
    }
}
