using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Scu;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueIT.v2;

public class ProtocolCommandProcessorIT : IProtocolCommandProcessor
{
    private readonly IITSSCD _itSSCD;
    private readonly IMiddlewareQueueItemRepository _queueItemRepository;

    public ProtocolCommandProcessorIT(IITSSCD itSSCD, IMiddlewareQueueItemRepository queueItemRepository)
    {
        _itSSCD = itSSCD;
        _queueItemRepository = queueItemRepository;
    }

    public async Task<ProcessCommandResponse> ProtocolUnspecified0x3000Async(ProcessCommandRequest request)
    {
        if (((long) request.ReceiptRequest.ftReceiptCase & 0x0000_0002_0000_0000) == 0)
        {
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        try
        {
            var v1Request = V1ScuAdapter.ToV1ProcessRequest(request.ReceiptRequest, request.ReceiptResponse);
            var result = await _itSSCD.ProcessReceiptAsync(v1Request).ConfigureAwait(false);
            V1ScuAdapter.MergeIntoV2(request.ReceiptResponse, result.ReceiptResponse);
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }
        catch (Exception ex)
        {
            request.ReceiptResponse.SetReceiptResponseError(ex.Message);
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }
    }

    public Task<ProcessCommandResponse> ProtocolTechnicalEvent0x3001Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> ProtocolAccountingEvent0x3002Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> InternalUsageMaterialConsumption0x3003Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> Order0x3004Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public Task<ProcessCommandResponse> Pay0x3005Async(ProcessCommandRequest request)
        => Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>()));

    public async Task<ProcessCommandResponse> CopyReceiptPrintExistingReceipt0x3010Async(ProcessCommandRequest request)
    {
        await MiddlewareStorageHelpers.LoadReceiptReferencesToResponse(_queueItemRepository, request.ReceiptRequest, request.ReceiptRequest.cbReceiptMoment, request.ReceiptResponse);
        if (request.ReceiptResponse.ftState.IsState(State.Error))
        {
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }

        try
        {
            var v1Request = V1ScuAdapter.ToV1ProcessRequest(request.ReceiptRequest, request.ReceiptResponse);
            var result = await _itSSCD.ProcessReceiptAsync(v1Request).ConfigureAwait(false);
            V1ScuAdapter.MergeIntoV2(request.ReceiptResponse, result.ReceiptResponse);
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }
        catch (Exception ex)
        {
            request.ReceiptResponse.SetReceiptResponseError(ex.Message);
            return new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>());
        }
    }
}
