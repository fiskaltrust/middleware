using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.QueueES.Validation;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueES.Processors;

public class InvoiceCommandProcessorES(ILogger<InvoiceCommandProcessorES> logger, AsyncLazy<IESSSCD> essscd, AsyncLazy<IConfigurationRepository> configurationRepository, AsyncLazy<IMiddlewareQueueItemRepository> queueItemRepository, AsyncLazy<IMiddlewareJournalESRepository> journalESRepository) : IInvoiceCommandProcessor
{
#pragma warning disable
    private readonly ILogger<InvoiceCommandProcessorES> _logger = logger;
    private readonly AsyncLazy<IESSSCD> _essscd = essscd;
    private readonly AsyncLazy<IConfigurationRepository> _configurationRepository = configurationRepository;
    private readonly AsyncLazy<IMiddlewareQueueItemRepository> _queueItemRepository = queueItemRepository;
    private readonly AsyncLazy<IMiddlewareJournalESRepository> _journalESRepository = journalESRepository;
#pragma warning restore

    public async Task<ProcessCommandResponse> InvoiceUnknown0x1000Async(ProcessCommandRequest request) => await FullInvoiceRequestAsync(request);

    public async Task<ProcessCommandResponse> InvoiceB2C0x1001Async(ProcessCommandRequest request) => await FullInvoiceRequestAsync(request);

    public async Task<ProcessCommandResponse> InvoiceB2B0x1002Async(ProcessCommandRequest request) => await FullInvoiceRequestAsync(request);

    public async Task<ProcessCommandResponse> InvoiceB2G0x1003Async(ProcessCommandRequest request) => await FullInvoiceRequestAsync(request);

    public async Task<ProcessCommandResponse> FullInvoiceRequestAsync(ProcessCommandRequest request)
    {
        var validator = new ReceiptValidator(request.ReceiptRequest, request.ReceiptResponse, _queueItemRepository);
        var validationResults = await validator.ValidateAndCollectAsync(new ReceiptValidationContext
        {
            IsRefund = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund),
            GeneratesSignature = true,
            IsHandwritten = request.ReceiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.HandWritten),
            //NumberSeries = series  // Include series for moment order validation
        });
        if (!validationResults.IsValid)
        {
            foreach (var result in validationResults.Results)
            {
                foreach (var error in result.Errors)
                {
                    request.ReceiptResponse.SetReceiptResponseError($"Validation error [{error.Code}]: {error.Message} (Field: {error.Field}, Index: {error.ItemIndex})");
                }
            }
            return new ProcessCommandResponse(request.ReceiptResponse, []);
        }


        var queueItemRepository = await _queueItemRepository;
        var queueES = await (await _configurationRepository).GetQueueESAsync(request.queue.ftQueueId);
        var lastQueueItem = queueES.LastInvoiceQueueItemId is not null ? await queueItemRepository.GetAsync(queueES.LastInvoiceQueueItemId.Value) : null;

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

        var lastReceipt = lastQueueItem is null ? null : new v2.Models.Receipt
        {
            Request = JsonSerializer.Deserialize<ReceiptRequest>(lastQueueItem.request)!,
            Response = JsonSerializer.Deserialize<ReceiptResponse>(lastQueueItem.response)!,
        };
        if (lastReceipt is not null)
        {
            lastReceipt.Response.ftStateData = null;
        }

        var responseStateData = MiddlewareStateData.FromReceiptResponse(request.ReceiptResponse);
        responseStateData.ES = new MiddlewareStateDataES
        {
            LastReceipt = lastReceipt,
        };
        
        // Generate series identifier if not set
        var serieFactura = queueES.InvoiceSeries ?? $"fkt{Helper.ShortGuid(request.queue.ftQueueId)}1000";
        var numFactura = queueES.InvoiceNumerator + 1;

        request.ReceiptResponse.ftReceiptIdentification += $"{serieFactura}-{numFactura}";
        request.ReceiptResponse.ftStateData = responseStateData;

        var response = await (await _essscd).ProcessReceiptAsync(new ProcessRequest
        {
            ReceiptRequest = request.ReceiptRequest,
            ReceiptResponse = request.ReceiptResponse
        });

        try
        {
            responseStateData = JsonSerializer.Deserialize<MiddlewareStateData>(((JsonElement) response.ReceiptResponse.ftStateData!).GetRawText())!;
            await (await _journalESRepository).InsertAsync(new ftJournalES
            {
                ftJournalESId = Guid.NewGuid(),
                Number = response.ReceiptResponse.ftQueueRow,
                Data = JsonSerializer.Serialize(responseStateData.ES!.GovernmentAPI),
                JournalType = JournalESType.TicketBAI.ToString(),
                ftQueueItemId = response.ReceiptResponse.ftQueueItemID,
                ftQueueId = response.ReceiptResponse.ftQueueID,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing receipt");
        }

        if (response.ReceiptResponse.ftState.IsState(State.Success))
        {
            queueES.SSCDSignQueueItemId = response.ReceiptResponse.ftQueueItemID;
            queueES.InvoiceNumerator = numFactura;
            queueES.InvoiceSeries = serieFactura;
            queueES.LastInvoiceMoment = request.ReceiptRequest.cbReceiptMoment;
            queueES.LastInvoiceQueueItemId = response.ReceiptResponse.ftQueueItemID;
            await (await _configurationRepository).InsertOrUpdateQueueESAsync(queueES);
        }
        return await Task.FromResult(new ProcessCommandResponse(response.ReceiptResponse, new List<ftActionJournal>())).ConfigureAwait(false);
    }
}

public class Helper
{
    public static string ShortGuid(Guid guid, int bytes = 8)
    {
        var guidBytes = guid.ToByteArray();
        var slice = new byte[bytes];
        Array.Copy(guidBytes, slice, bytes);
        return Convert.ToBase64String(slice)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}