using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.Cases;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.es;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class ReceiptCommandProcessorES(IESSSCD sscd, IConfigurationRepository configurationRepository, IReadOnlyQueueItemRepository queueItemRepository) : IReceiptCommandProcessor
{
#pragma warning disable
    private readonly IESSSCD _sscd = sscd;
    private readonly IConfigurationRepository _configurationRepository = configurationRepository;
    private readonly IReadOnlyQueueItemRepository _queueItemRepository = queueItemRepository;
#pragma warning restore

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        var queueES = await _configurationRepository.GetQueueESAsync(request.queue.ftQueueId);
        var previousQueueItem = queueES.SSCDSignQueueItemId is not null ? await _queueItemRepository.GetAsync(queueES.SSCDSignQueueItemId.Value) : null;

        if (previousQueueItem is not null)
        {
            if (previousQueueItem?.request is null)
            {
                throw new Exception("Previous queue item request is null");
            }

            if (previousQueueItem?.request is null)
            {
                throw new Exception("Previous queue item request is null");
            }
        }

        var response = await _sscd.ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            PreviousReceiptRequest = previousQueueItem is null ? null : JsonSerializer.Deserialize<ReceiptRequest>(previousQueueItem.request)!, // handle null case?
            PreviousReceiptResponse = previousQueueItem is null ? null : JsonSerializer.Deserialize<ReceiptResponse>(previousQueueItem.response)!,
        });

        queueES.SSCDSignQueueItemId = response.ReceiptResponse.ftQueueItemID;
        await _configurationRepository.InsertOrUpdateQueueESAsync(queueES);

        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}
