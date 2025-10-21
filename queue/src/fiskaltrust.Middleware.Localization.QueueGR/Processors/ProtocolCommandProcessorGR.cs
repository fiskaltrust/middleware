using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.gr;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class ProtocolCommandProcessorGR(IGRSSCD sscd) : IProtocolCommandProcessor
{
    private readonly IGRSSCD _sscd = sscd;

    public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);

    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request) => GRFallBackOperations.NotSupported(request, "InternalUsageMaterialConsumption");

    public async Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request)
    {
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        }, []);
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);

    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request) => await GRFallBackOperations.NoOp(request);
}