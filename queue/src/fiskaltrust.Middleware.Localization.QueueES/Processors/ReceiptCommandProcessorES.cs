using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using fiskaltrust.ifPOS.v2.Cases;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Contracts.Repositories;
using System.Text.Json.Nodes;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class ReceiptCommandProcessorES(AsyncLazy<IESSSCD> essscd, AsyncLazy<IConfigurationRepository> configurationRepository, AsyncLazy<IMiddlewareQueueItemRepository> queueItemRepository) : IReceiptCommandProcessor
{
#pragma warning disable
    private readonly AsyncLazy<IESSSCD> _essscd = essscd;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItemRepository = queueItemRepository;
#pragma warning restore

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        var queueItemRepository = await _queueItemRepository;
        var queueES = await (await _configurationRepository).GetQueueESAsync(request.queue.ftQueueId);
        var previousQueueItem = queueES.SSCDSignQueueItemId is not null ? await queueItemRepository.GetAsync(queueES.SSCDSignQueueItemId.Value) : null;

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

        ftQueueItem? referencedQueueItem = null;
        if (request.ReceiptRequest.cbPreviousReceiptReference is not null)
        {
            if (request.ReceiptRequest.cbPreviousReceiptReference.IsGroup)
            {
                throw new Exception("cbPreviousReceiptReference cannot be a group reference are not supported.");
            }

            var referencedQueueItems = await queueItemRepository.GetByReceiptReferenceAsync(request.ReceiptRequest.cbPreviousReceiptReference.SingleValue).ToListAsync();
            if (!referencedQueueItems.Any())
            {
                throw new Exception($"Referenced queue item with reference {request.ReceiptRequest.cbPreviousReceiptReference.SingleValue} not found.");
            }
            if (referencedQueueItems.Count > 1)
            {
                throw new Exception($"Multiple queue items found with reference {request.ReceiptRequest.cbPreviousReceiptReference.SingleValue}.");
            }
            referencedQueueItem = referencedQueueItems.Single();
        }

        if (request.ReceiptRequest.ftReceiptCaseData is not null)
        {
            var jsonNode = JsonNode.Parse(((JsonElement) request.ReceiptRequest.ftReceiptCaseData).GetRawText())!;
            var jsonObject = jsonNode.GetValueKind() switch
            {
                JsonValueKind.String => JsonNode.Parse(jsonNode.GetValue<string>())!.AsObject(),
                JsonValueKind.Object => jsonNode.AsObject(),
                _ => throw new Exception("ftReceiptCaseData must be a string or an object.")
            };

            jsonObject["IESSSCD"] = JsonSerializer.SerializeToNode(new
            {
                LastReceiptRequest = previousQueueItem is null ? null : JsonSerializer.Deserialize<ReceiptRequest>(previousQueueItem.request)!,
                LastReceiptResponse = previousQueueItem is null ? null : JsonSerializer.Deserialize<ReceiptResponse>(previousQueueItem.response)!,
            });
            request.ReceiptRequest.ftReceiptCaseData = JsonSerializer.Deserialize<JsonElement>(jsonObject.ToJsonString());
        }

        var response = await (await _essscd).ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            ReferencedReceiptRequest = referencedQueueItem is null ? null : JsonSerializer.Deserialize<ReceiptRequest>(referencedQueueItem.request)!,
            ReferencedReceiptResponse = referencedQueueItem is null ? null : JsonSerializer.Deserialize<ReceiptResponse>(referencedQueueItem.response)!,
        });

        queueES.SSCDSignQueueItemId = response.ReceiptResponse.ftQueueItemID;
        await (await _configurationRepository).InsertOrUpdateQueueESAsync(queueES);

        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}
