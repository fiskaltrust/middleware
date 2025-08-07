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
using System.Text.Json.Serialization;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class ReceiptCommandProcessorES(ILogger<ReceiptCommandProcessorES> logger, AsyncLazy<IESSSCD> essscd, AsyncLazy<IConfigurationRepository> configurationRepository, AsyncLazy<IMiddlewareQueueItemRepository> queueItemRepository, AsyncLazy<IMiddlewareJournalESRepository> journalESRepository) : IReceiptCommandProcessor
{
#pragma warning disable
    private readonly ILogger<ReceiptCommandProcessorES> _logger = logger;
    private readonly AsyncLazy<IESSSCD> _essscd = essscd;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItemRepository = queueItemRepository;
    private readonly AsyncLazy<IMiddlewareJournalESRepository> _journalESRepository = journalESRepository;
#pragma warning restore

    public async Task<ProcessCommandResponse> UnknownReceipt0x0000Async(ProcessCommandRequest request) => await PointOfSaleReceipt0x0001Async(request);

    public async Task<ProcessCommandResponse> PointOfSaleReceipt0x0001Async(ProcessCommandRequest request)
    {
        var queueItemRepository = await _queueItemRepository;
        var queueES = await (await _configurationRepository).GetQueueESAsync(request.queue.ftQueueId);
        var lastQueueItem = queueES.SSCDSignQueueItemId is not null ? await queueItemRepository.GetAsync(queueES.SSCDSignQueueItemId.Value) : null;

        if (lastQueueItem is not null)
        {
            if (lastQueueItem?.request is null)
            {
                throw new Exception("Last queue item request is null");
            }

            if (lastQueueItem?.request is null)
            {
                throw new Exception("Last queue item request is null");
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

        if (request.ReceiptResponse.ftStateData is not null)
        {
            throw new Exception("ftStateData must be empty.");
        }

        var lastReceipt = lastQueueItem is null ? null : new LastReceipt
        {
            Request = JsonSerializer.Deserialize<ReceiptRequest>(lastQueueItem.request)!,
            Response = JsonSerializer.Deserialize<ReceiptResponse>(lastQueueItem.response)!,
        };
        if (lastReceipt is not null)
        {
            lastReceipt.Response.ftStateData = null;
        }
        request.ReceiptResponse.ftStateData = new MiddlewareState
        {
            ES = new MiddlewareQueueESState
            {
                LastReceipt = lastReceipt
            }
        };

        var response = await (await _essscd).ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse,
            ReferencedReceiptRequest = referencedQueueItem is null ? null : JsonSerializer.Deserialize<ReceiptRequest>(referencedQueueItem.request)!,
            ReferencedReceiptResponse = referencedQueueItem is null ? null : JsonSerializer.Deserialize<ReceiptResponse>(referencedQueueItem.response)!,
        });

        try
        {
            var responseStateData = JsonSerializer.Deserialize<MiddlewareState>(((JsonElement) response.ReceiptResponse.ftStateData!).GetRawText())!;
            await (await _journalESRepository).InsertAsync(new ftJournalES
            {
                ftJournalESId = Guid.NewGuid(),
                Number = response.ReceiptResponse.ftQueueRow,
                Data = JsonSerializer.Serialize(responseStateData.ES!.GovernmentAPI),
                JournalType = JournalESType.VeriFactu.ToString(),
                ftQueueItemId = response.ReceiptResponse.ftQueueItemID,
                ftQueueId = response.ReceiptResponse.ftQueueID,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing receipt");
        }

        queueES.SSCDSignQueueItemId = response.ReceiptResponse.ftQueueItemID;
        await (await _configurationRepository).InsertOrUpdateQueueESAsync(queueES);

        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }

    public async Task<ProcessCommandResponse> PaymentTransfer0x0002Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> PointOfSaleReceiptWithoutObligation0x0003Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> ECommerce0x0004Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);

    public async Task<ProcessCommandResponse> DeliveryNote0x0005Async(ProcessCommandRequest request) => await Task.FromResult(new ProcessCommandResponse(request.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
}
