using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Factories;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

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

    /// <summary>
    /// A technical event is an entry of the NF525 technical event journal (JET, Infocert R19 v4.2):
    /// its code must be one R19 defines or an editor code, and it must carry a label. A malformed
    /// event is rejected before it takes a number of chain L, so the journal never holds an entry
    /// an auditor cannot read (market-fr#200). Training events are not journal entries.
    /// </summary>
    public Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request)
    {
        if (FRJetEvent.IsJetEvent(request.ReceiptRequest) && !FRJetEvent.TryRead(request.ReceiptRequest, out _, out var error))
        {
            request.ReceiptResponse.SetReceiptResponseError($"The technical event cannot enter the NF525 technical event journal: {error}");
            return Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));
        }

        return _pipeline.SignAsync(request);
    }

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
