using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2;

public class SignProcessor : ISignProcessor
{
    private readonly ILogger<SignProcessor> _logger;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IMiddlewareQueueItemRepository _queueItemRepository;
    private readonly IMiddlewareReceiptJournalRepository _receiptJournalRepository;
    private readonly IMiddlewareActionJournalRepository _actionJournalRepository;
    private readonly Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)>> _processRequest;
    private readonly string _cashBoxIdentification;
    private readonly Guid _queueId = Guid.Empty;
    private readonly Guid _cashBoxId = Guid.Empty;
    private readonly bool _isSandbox;
    private readonly QueueStorageProvider _queueStorageProvider;
    private readonly int _receiptRequestMode = 0;

    public SignProcessor(
        ILogger<SignProcessor> logger,
        IStorageProvider storageProvider,
        Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)>> processRequest,
        string cashBoxIdentification,
        MiddlewareConfiguration configuration)
    {
        _logger = logger;
        _configurationRepository = storageProvider.GetConfigurationRepository();
        _queueItemRepository = storageProvider.GetMiddlewareQueueItemRepository();
        _receiptJournalRepository = storageProvider.GetMiddlewareReceiptJournalRepository();
        _actionJournalRepository = storageProvider.GetMiddlewareActionJournalRepository();
        _processRequest = processRequest;
        _cashBoxIdentification = cashBoxIdentification;
        _queueId = configuration.QueueId;
        _cashBoxId = configuration.CashBoxId;
        _isSandbox = configuration.IsSandbox;
        _queueStorageProvider = new QueueStorageProvider(_queueId, _configurationRepository, _queueItemRepository, _receiptJournalRepository);
        _receiptRequestMode = configuration.ReceiptRequestMode;
    }

    public async Task<ReceiptResponse?> ProcessAsync(ReceiptRequest request)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.ftCashBoxID != _cashBoxId)
            {
                throw new Exception("Provided CashBoxId does not match current CashBoxId");
            }

            if ((request.ftReceiptCase & 0x0000800000000000L) > 0)
            {
                ReceiptResponse? receiptResponseFound = null;
                try
                {
                    var foundQueueItem = await GetExistingQueueItemOrNullAsync(request).ConfigureAwait(false);
                    if (foundQueueItem != null)
                    {
                        var message = $"Queue {_queueId} found cbReceiptReference \"{foundQueueItem.cbReceiptReference}\"";
                        _logger.LogWarning(message);
                        await CreateActionJournalAsync(message, "", foundQueueItem.ftQueueItemId).ConfigureAwait(false);
                        receiptResponseFound = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(foundQueueItem.response);
                    }
                }
                catch (Exception x)
                {
                    var message = $"Queue {_queueId} problem on receitrequest";
                    _logger.LogError(x, message);
                    await CreateActionJournalAsync(message, "", null).ConfigureAwait(false);
                }


                if (receiptResponseFound != null)
                {
                    return receiptResponseFound;
                }
                else
                {
                    if (_receiptRequestMode == 1)
                    {
                        //try to sign, remove receiptrequest-flag
                        request.ftReceiptCase -= 0x0000800000000000L;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            var queue = await _configurationRepository.GetQueueAsync(_queueId).ConfigureAwait(false);
            return await InternalSign(request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "");
            throw;
        }
    }

    private async Task<ReceiptResponse?> InternalSign(ReceiptRequest data)
    {
        var queueItem = await _queueStorageProvider.ReserverNextQueueItem(data);
        var actionjournals = new List<ftActionJournal>();
        try
        {
            queueItem.ftWorkMoment = DateTime.UtcNow;
            _logger.LogTrace("SignProcessor.InternalSign: Calling country specific SignProcessor.");
            var receiptIdentification = $"ft{_queueStorageProvider.GetReceiptNumerator():X}#";
            var receiptResponse = new ReceiptResponse
            {
                ftCashBoxID = data.ftCashBoxID,
                ftQueueID = queueItem.ftQueueId,
                ftQueueItemID = queueItem.ftQueueItemId,
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = data.cbTerminalID,
                cbReceiptReference = data.cbReceiptReference,
                ftCashBoxIdentification = _cashBoxIdentification,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (long) ((ulong) data.ftReceiptCase & 0xFFFF_F000_0000_0000),
                ftReceiptIdentification = receiptIdentification
            };

            List<ftActionJournal> countrySpecificActionJournals;
            try
            {
                (receiptResponse, countrySpecificActionJournals) = await ProcessAsync(data, receiptResponse, queueItem).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                countrySpecificActionJournals = new();
                receiptResponse.HasFailed();
                receiptResponse.AddSignatureItem(new SignatureItem
                {
                    ftSignatureFormat = 0x1,
                    ftSignatureType = (long) (((ulong) data.ftReceiptCase & 0xFFFF_0000_0000_0000) | 0x2000_0000_3000),
                    Caption = "uncaught-exeption",
                    Data = e.ToString()
                });
            }
            actionjournals.AddRange(countrySpecificActionJournals);
            if (_isSandbox)
            {
                receiptResponse.ftSignatures.Add(SignatureFactory.CreateSandboxSignature(_queueId));
            }
            await _queueStorageProvider.FinishQueueItem(queueItem, receiptResponse);
            if ((receiptResponse.ftState & 0xFFFF_FFFF) == 0xEEEE_EEEE)
            {
                var errorMessage = "An error occurred during receipt processing, resulting in ftState = 0xEEEE_EEEE.";
                await CreateActionJournalAsync(errorMessage, $"{receiptResponse.ftState:X}", queueItem.ftQueueItemId);
                return receiptResponse;
            }
            else
            {
                _ = await _queueStorageProvider.InsertReceiptJournal(queueItem, data);
            }
            return receiptResponse;
        }
        finally
        {
            foreach (var actionJournal in actionjournals)
            {
                await _actionJournalRepository.InsertAsync(actionJournal);
            }
        }
    }

    private async Task<ftQueueItem?> GetExistingQueueItemOrNullAsync(ReceiptRequest data)
    {
        _logger.LogTrace("SignProcessor.GetExistingQueueItemOrNullAsync called.");
        var queueItems = (await _queueItemRepository.GetByReceiptReferenceAsync(data.cbReceiptReference, data.cbTerminalID).ToListAsync().ConfigureAwait(false))
            .OrderByDescending(x => x.TimeStamp);

        foreach (var existingQueueItem in queueItems)
        {
            if (!IsReceiptRequestFinished(existingQueueItem))
            {
                continue;
            }
            if (IsContentOfQueueItemEqualWithGivenRequest(data, existingQueueItem))
            {
                return existingQueueItem;
            }
        }
        return null;
    }

    public async Task CreateActionJournalAsync(string message, string type, Guid? queueItemId)
    {
        var actionJournal = new ftActionJournal
        {
            ftActionJournalId = Guid.NewGuid(),
            ftQueueId = _queueId,
            ftQueueItemId = queueItemId.GetValueOrDefault(),
            Message = message,
            Priority = 0,
            Type = type,
            Moment = DateTime.UtcNow
        };

        await _actionJournalRepository.InsertAsync(actionJournal).ConfigureAwait(false);
    }

    private static bool IsContentOfQueueItemEqualWithGivenRequest(ReceiptRequest data, ftQueueItem item)
    {
        var itemRequest = JsonConvert.DeserializeObject<ReceiptRequest>(item.request);
        if (itemRequest.cbChargeItems.Count == data.cbChargeItems.Count && itemRequest.cbPayItems.Count == data.cbPayItems.Count)
        {
            for (var i = 0; i < itemRequest.cbChargeItems.Count; i++)
            {
                if (itemRequest.cbChargeItems[i].Amount != data.cbChargeItems[i].Amount)
                {
                    return false;
                }
                if (itemRequest.cbChargeItems[i].ftChargeItemCase != data.cbChargeItems[i].ftChargeItemCase)
                {
                    return false;
                }
                if (itemRequest.cbChargeItems[i].Moment != data.cbChargeItems[i].Moment)
                {
                    return false;
                }
            }
            for (var i = 0; i < itemRequest.cbPayItems.Count; i++)
            {
                if (itemRequest.cbPayItems[i].Amount != data.cbPayItems[i].Amount)
                {
                    return false;
                }
                if (itemRequest.cbPayItems[i].ftPayItemCase != data.cbPayItems[i].ftPayItemCase)
                {
                    return false;
                }
                if (itemRequest.cbPayItems[i].Moment != data.cbPayItems[i].Moment)
                {
                    return false;
                }
            }
        }
        else
        {
            return false;
        }
        return true;
    }

    private static bool IsReceiptRequestFinished(ftQueueItem item) => item.ftDoneMoment != null && !string.IsNullOrWhiteSpace(item.response) && !string.IsNullOrWhiteSpace(item.responseHash);

    public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
    {
        var queue = await _queueStorageProvider.GetQueueAsync();
        if (queue.IsDeactivated())
        {
            return ReturnWithQueueIsDisabled(queue, receiptResponse, queueItem);
        }

        if (request.IsInitialOperation() && !queue.IsNew())
        {
            receiptResponse.SetReceiptResponseError("The queue is already operational. It is not allowed to send another InitOperation Receipt");
            return (receiptResponse, new List<ftActionJournal>());
        }

        if (!request.IsInitialOperation() && queue.IsNew())
        {
            return ReturnWithQueueIsNotActive(queue, receiptResponse, queueItem);
        }
        return await _processRequest(request, receiptResponse, queue, queueItem).ConfigureAwait(false);
    }

    private (ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals) ReturnWithQueueIsNotActive(ftQueue queue, ReceiptResponse receiptResponse, ftQueueItem queueItem)
    {
        receiptResponse.MarkAsDisabled();
        receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
        return (receiptResponse, [
            new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queueItem.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Moment = DateTime.UtcNow,
                Message = $"QueueId {queueItem.ftQueueId} has not been activated yet."
            }]);
    }

    private (ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals) ReturnWithQueueIsDisabled(ftQueue queue, ReceiptResponse receiptResponse, ftQueueItem queueItem)
    {
        receiptResponse.MarkAsDisabled();
        receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
        return (receiptResponse, [ new() {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queueItem.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                Moment = DateTime.UtcNow,
                Message = $"QueueId {queueItem.ftQueueId} has been disabled."
            }]);
    }
}