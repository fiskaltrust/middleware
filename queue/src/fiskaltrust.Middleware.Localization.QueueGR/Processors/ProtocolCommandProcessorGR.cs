using fiskaltrust.Middleware.Localization.QueueGR.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
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

    public async Task<ProcessCommandResponse> ProcessReceiptAsync(ProcessCommandRequest request)
    {
        var receiptCase = request.ReceiptRequest.ftReceiptCase.Case();
        switch (receiptCase)
        {
            case ReceiptCase.ProtocolUnspecified0x3000:
                return await ProtocolUnspecified0x3000Async(request);
            case ReceiptCase.ProtocolTechnicalEvent0x3001:
                return await ProtocolTechnicalEvent0x3001Async(request);
            case ReceiptCase.ProtocolAccountingEvent0x3002:
                return await ProtocolAccountingEvent0x3002Async(request);
            case ReceiptCase.InternalUsageMaterialConsumption0x3003:
                return await InternalUsageMaterialConsumption0x3003Async(request);
            case ReceiptCase.Order0x3004:
                return await Order0x3004Async(request);
            case ReceiptCase.CopyReceiptPrintExistingReceipt0x3010:
                return await CopyReceiptPrintExistingReceipt0x3010Async(request);
            case (ReceiptCase) 0x3005:
                return await Order0x3004Async(request);
            case (ReceiptCase) 0x3006:
                return await Order0x3004Async(request);
        }
        request.ReceiptResponse.SetReceiptResponseError(ErrorMessages.UnknownReceiptCase((long) request.ReceiptRequest.ftReceiptCase));
        return new ProcessCommandResponse(request.ReceiptResponse, []);
    }

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

    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}