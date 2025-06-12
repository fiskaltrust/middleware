using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.v2;

public class SignProcessor : ISignProcessor
{
    private readonly ILogger<SignProcessor> _logger;
    private readonly Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)>> _processRequest;
    private readonly string _cashBoxIdentification;
    private readonly Guid _queueId = Guid.Empty;
    private readonly Guid _cashBoxId = Guid.Empty;
    private readonly bool _isSandbox;
    private readonly QueueStorageProvider _queueStorageProvider;
    private readonly int _receiptRequestMode = 0;

    public SignProcessor(
        ILogger<SignProcessor> logger,
        QueueStorageProvider queueStorageProvider,
        Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)>> processRequest,
        string cashBoxIdentification,
        MiddlewareConfiguration configuration)
    {
        _logger = logger;
        _processRequest = processRequest;
        _cashBoxIdentification = cashBoxIdentification;
        _queueId = configuration.QueueId;
        _cashBoxId = configuration.CashBoxId;
        _isSandbox = configuration.IsSandbox;
        _queueStorageProvider = queueStorageProvider;
        _receiptRequestMode = configuration.ReceiptRequestMode;
    }

    public async Task<ReceiptResponse?> ProcessAsync(ReceiptRequest receiptRequest)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(receiptRequest);
            if (receiptRequest.ftCashBoxID != _cashBoxId)
            {
                throw new Exception("Provided CashBoxId does not match current CashBoxId");
            }

            if (fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlagsExt.IsFlag(receiptRequest.ftReceiptCase, fiskaltrust.ifPOS.v2.Cases.ReceiptCaseFlags.ReceiptRequested))
            {
                ReceiptResponse? receiptResponseFound = null;
                try
                {
                    var foundQueueItem = await _queueStorageProvider.GetExistingQueueItemOrNullAsync(receiptRequest).ConfigureAwait(false);
                    if (foundQueueItem != null)
                    {
                        var message = $"Queue {_queueId} found cbReceiptReference \"{foundQueueItem.cbReceiptReference}\"";
                        _logger.LogWarning(message);
                        await _queueStorageProvider.CreateActionJournalAsync(message, "", foundQueueItem.ftQueueItemId).ConfigureAwait(false);
                        receiptResponseFound = System.Text.Json.JsonSerializer.Deserialize<ReceiptResponse>(foundQueueItem.response);
                    }
                }
                catch (Exception x)
                {
                    var message = $"Queue {_queueId} problem on receitrequest";
                    _logger.LogError(x, message);
                    await _queueStorageProvider.CreateActionJournalAsync(message, "", null).ConfigureAwait(false);
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
                        receiptRequest.ftReceiptCase -= 0x0000800000000000L;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            var actionjournals = new List<ftActionJournal>();
            try
            {
                var queueItem = await _queueStorageProvider.ReserveNextQueueItem(receiptRequest);
                queueItem.ftWorkMoment = DateTime.UtcNow;
                var receiptResponse = CreateReceiptResponse(receiptRequest, queueItem);
                receiptResponse.ftReceiptIdentification = $"ft{await _queueStorageProvider.GetReceiptNumerator():X}#";
                List<ftActionJournal> countrySpecificActionJournals;
                try
                {
                    (receiptResponse, countrySpecificActionJournals) = await ProcessAsync(receiptRequest, receiptResponse, queueItem).ConfigureAwait(false);
                    actionjournals.AddRange(countrySpecificActionJournals);
                }
                catch (Exception e)
                {
                    receiptResponse.HasFailed();
                    receiptResponse.AddSignatureItem(new SignatureItem
                    {
                        ftSignatureFormat = SignatureFormat.Text,
                        ftSignatureType = receiptRequest.ftReceiptCase.Reset().As<SignatureType>().WithCategory(SignatureTypeCategory.Failure),
                        Caption = "uncaught-exeption",
                        Data = e.ToString()
                    });
                }
                if (_isSandbox)
                {
                    receiptResponse.ftSignatures.Add(SignatureFactory.CreateSandboxSignature(_queueId));
                }

                await _queueStorageProvider.FinishQueueItem(queueItem, receiptResponse);

                if (receiptResponse.ftState.IsState(State.Error))
                {
                    var errorMessage = "An error occurred during receipt processing, resulting in ftState = 0xEEEE_EEEE.";
                    await _queueStorageProvider.CreateActionJournalAsync(errorMessage, $"{receiptResponse.ftState:X}", queueItem.ftQueueItemId);
                    return receiptResponse;
                }
                else
                {
                    _ = await _queueStorageProvider.InsertReceiptJournal(queueItem, receiptRequest);
                }
                return receiptResponse;
            }
            finally
            {
                foreach (var actionJournal in actionjournals)
                {
                    await _queueStorageProvider.CreateActionJournalAsync(actionJournal);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "");
            throw;
        }
    }

    private ReceiptResponse CreateReceiptResponse(ReceiptRequest receiptRequest, ftQueueItem queueItem)
    {
        return new ReceiptResponse
        {
            ftCashBoxID = receiptRequest.ftCashBoxID,
            ftQueueID = queueItem.ftQueueId,
            ftQueueItemID = queueItem.ftQueueItemId,
            ftQueueRow = queueItem.ftQueueRow,
            cbTerminalID = receiptRequest.cbTerminalID,
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftCashBoxIdentification = _cashBoxIdentification,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) ((ulong) receiptRequest.ftReceiptCase & 0xFFFF_F000_0000_0000),
            ftReceiptIdentification = "",
        };
    }

    public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ReceiptResponse receiptResponse, ftQueueItem queueItem)
    {
        var queue = await _queueStorageProvider.GetQueueAsync();
        if (queue.IsDeactivated())
        {
            return ReturnWithQueueIsDisabled(queue, receiptResponse, queueItem);
        }

        if (request.ftReceiptCase.IsCase(ReceiptCase.InitialOperationReceipt0x4001) && !queue.IsNew())
        {
            receiptResponse.SetReceiptResponseError("The queue is already operational. It is not allowed to send another InitOperation Receipt");
            return (receiptResponse, new List<ftActionJournal>());
        }

        if (!request.ftReceiptCase.IsCase(ReceiptCase.InitialOperationReceipt0x4001) && queue.IsNew())
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
