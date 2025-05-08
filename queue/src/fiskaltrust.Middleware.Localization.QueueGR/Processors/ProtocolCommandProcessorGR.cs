using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD;
using fiskaltrust.Middleware.Storage.GR;

namespace fiskaltrust.Middleware.Localization.QueueGR.Processors;

public class ProtocolCommandProcessorGR(IGRSSCD sscd, ftQueueGR queueGR, ftSignaturCreationUnitGR signaturCreationUnitGR) : IProtocolCommandProcessor
{
#pragma warning disable
    private readonly IGRSSCD _sscd = sscd;
    private readonly ftQueueGR _queueGR = queueGR;
    private readonly ftSignaturCreationUnitGR _signaturCreationUnitGR = signaturCreationUnitGR;
#pragma warning restore

    public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request)
    {
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request)
    {
        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
        });
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request)
    {
        return await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}