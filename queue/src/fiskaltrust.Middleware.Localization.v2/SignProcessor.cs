using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Configuration;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.PostFiscalization;

namespace fiskaltrust.Middleware.Localization.v2;

public class SignProcessor : ISignProcessor
{
    private static readonly JsonSerializerOptions _journalSerializerOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly ILogger<SignProcessor> _logger;
    private readonly Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)>> _processRequest;
    private readonly AsyncLazy<string> _cashBoxIdentification;
    private readonly Guid _queueId = Guid.Empty;
    private readonly Guid _cashBoxId = Guid.Empty;
    private readonly bool _isSandbox;
    private readonly IQueueStorageProvider _queueStorageProvider;
    private readonly int _receiptRequestMode = 0;
    private readonly PostFiscalizationProcessor _postFiscalizationProcessor;

    public SignProcessor(
        ILogger<SignProcessor> logger,
        IQueueStorageProvider queueStorageProvider,
        Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)>> processRequest,
        AsyncLazy<string> cashBoxIdentification,
        MiddlewareConfiguration configuration,
        PostFiscalizationProcessor postFiscalizationProcessor)
    {
        _logger = logger;
        _processRequest = processRequest;
        _cashBoxIdentification = cashBoxIdentification;
        _queueId = configuration.QueueId;
        _cashBoxId = configuration.CashBoxId;
        _isSandbox = configuration.IsSandbox;
        _queueStorageProvider = queueStorageProvider;
        _receiptRequestMode = configuration.ReceiptRequestMode;
        _postFiscalizationProcessor = postFiscalizationProcessor;
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

            if (receiptRequest.ftReceiptCase.IsFlag(ReceiptCaseFlags.ReceiptRequested))
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
                        receiptResponseFound = JsonSerializer.Deserialize<ReceiptResponse>(foundQueueItem.response);
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
                    // Replays return the persisted response; neither preflight nor finalize runs again.
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

            // RFC 712, phase 1: the configured eInvoicing/eReporting services validate the receipt before any bookkeeping
            // that produces a fiscal record. A rejection or an unreachable service refuses the receipt: no queue item, no
            // fiscal record, no receipt journal, so the POS can correct and resend it as a new transaction.
            var preflight = await _postFiscalizationProcessor.ValidateAsync(receiptRequest).ConfigureAwait(false);
            if (!preflight.Accepted)
            {
                return await RejectBeforeFiscalizationAsync(receiptRequest, preflight.Rejection!).ConfigureAwait(false);
            }

            var actionjournals = new List<ftActionJournal>();
            try
            {
                var queueItem = await _queueStorageProvider.ReserveNextQueueItem(receiptRequest);
                queueItem.ftWorkMoment = DateTime.UtcNow;
                var receiptResponse = CreateReceiptResponse(receiptRequest, queueItem, await _cashBoxIdentification);
                receiptResponse.ftReceiptIdentification = $"ft{await _queueStorageProvider.GetReceiptNumerator():X}#";

                List<ftActionJournal> countrySpecificActionJournals;
                try
                {
                    (receiptResponse, countrySpecificActionJournals) = await ProcessAsync(receiptRequest, receiptResponse, queueItem).ConfigureAwait(false);
                    actionjournals.AddRange(countrySpecificActionJournals);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Uncaught exception during receipt processing");

                    receiptResponse.MarkAsFailed();
                    receiptResponse.AddSignatureItem(new SignatureItem
                    {
                        ftSignatureFormat = SignatureFormat.Text,
                        ftSignatureType = receiptRequest.ftReceiptCase.Reset().As<SignatureType>().WithCategory(SignatureTypeCategory.Failure),
                        Caption = "uncaught-exeption",
                        Data = e.ToString()
                    });
                }

                // RFC 712, phase 3: the services that act on this receipt get the fully fiscalized response, only when
                // fiscalization succeeded. The receipt journal decision below is based on this fiscalization outcome,
                // not on the final ftState: a finalize failure marks a fiscalized receipt with the error state, and that
                // receipt is journaled like any other fiscalized receipt.
                var fiscalizationSucceeded = !receiptResponse.ftState.IsState(State.Error);
                if (fiscalizationSucceeded)
                {
                    receiptResponse = await _postFiscalizationProcessor.FinalizeAsync(receiptRequest, receiptResponse, queueItem, preflight, actionjournals).ConfigureAwait(false);
                }

                if (_isSandbox)
                {
                    receiptResponse.AddSignatureItem(SignatureFactory.CreateSandboxSignature(_queueId));
                }

                await _queueStorageProvider.FinishQueueItem(queueItem, receiptResponse);

                AddActivityTags(receiptRequest, receiptResponse);

                if (!fiscalizationSucceeded)
                {
                    var errorMessage = "An error occurred during receipt processing, resulting in ftState = 0xEEEE_EEEE.";
                    await _queueStorageProvider.CreateActionJournalAsync(errorMessage, $"{receiptResponse.ftState:X}", queueItem.ftQueueItemId);
                    return receiptResponse;
                }

                _ = await _queueStorageProvider.InsertReceiptJournal(queueItem, receiptRequest);
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

    private ReceiptResponse CreateReceiptResponse(ReceiptRequest receiptRequest, ftQueueItem queueItem, string cashBoxIdentification)
    {
        return new ReceiptResponse
        {
            ftCashBoxID = receiptRequest.ftCashBoxID,
            ftQueueID = queueItem.ftQueueId,
            ftQueueItemID = queueItem.ftQueueItemId,
            ftQueueRow = queueItem.ftQueueRow,
            cbTerminalID = receiptRequest.cbTerminalID,
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftCashBoxIdentification = cashBoxIdentification,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = State.Success.WithCountry(queueItem.country?.ToUpper()).WithVersion(0x2),
            ftReceiptIdentification = "",
        };
    }

    /// <summary>
    /// RFC 712: a configured eInvoicing/eReporting service declined the receipt, or could not be reached, in the preflight.
    /// Nothing is fiscalized and no queue item exists, so the response carries no queue item id. The POS gets a normal
    /// error response with a signature naming the reason, and an action journal entry records the technical detail.
    /// </summary>
    private async Task<ReceiptResponse> RejectBeforeFiscalizationAsync(ReceiptRequest receiptRequest, PostFiscalizationRejection rejection)
    {
        var queue = await _queueStorageProvider.GetQueueAsync().ConfigureAwait(false);
        var receiptResponse = new ReceiptResponse
        {
            ftCashBoxID = receiptRequest.ftCashBoxID,
            ftQueueID = _queueId,
            ftQueueItemID = Guid.Empty,
            ftQueueRow = 0,
            cbTerminalID = receiptRequest.cbTerminalID,
            cbReceiptReference = receiptRequest.cbReceiptReference,
            ftCashBoxIdentification = await _cashBoxIdentification,
            ftReceiptMoment = DateTime.UtcNow,
            ftState = State.Success.WithCountry(queue.CountryCode?.ToUpper()).WithVersion(0x2).WithState(State.Error),
            ftReceiptIdentification = "",
        };
        receiptResponse.AddSignatureItem(_postFiscalizationProcessor.CreateFailureSignature(receiptRequest, rejection.Caption, rejection.Reason));
        if (_isSandbox)
        {
            receiptResponse.AddSignatureItem(SignatureFactory.CreateSandboxSignature(_queueId));
        }

        var message = $"Receipt \"{receiptRequest.cbReceiptReference}\" was refused before fiscalization by the {rejection.Service.DisplayName()} service ({rejection.Caption}): {rejection.Reason}";
        _logger.LogWarning(message);
        await _queueStorageProvider.CreateActionJournalAsync(new ftActionJournal
        {
            ftActionJournalId = Guid.NewGuid(),
            ftQueueId = _queueId,
            ftQueueItemId = Guid.Empty,
            Moment = DateTime.UtcNow,
            Priority = 0x10,
            Type = rejection.Caption,
            Message = message,
            DataJson = JsonSerializer.Serialize(new
            {
                service = rejection.Service.Key(),
                phase = "validate",
                outcome = rejection.Outcome.ToTagValue(),
                reason = rejection.Reason,
                detail = rejection.Detail,
                cbReceiptReference = receiptRequest.cbReceiptReference,
                cbTerminalID = receiptRequest.cbTerminalID,
                ftReceiptCase = $"0x{receiptRequest.ftReceiptCase:X}",
            }, _journalSerializerOptions),
        }).ConfigureAwait(false);

        AddActivityTags(receiptRequest, receiptResponse);
        return receiptResponse;
    }

    private void AddActivityTags(ReceiptRequest receiptRequest, ReceiptResponse receiptResponse)
    {
        System.Diagnostics.Activity.Current?.AddTag("queue.ReceiptResponse.ftState", $"0x{receiptResponse.ftState:X}");
        System.Diagnostics.Activity.Current?.AddTag("queue.ReceiptRequest.ftReceiptCase", $"0x{receiptRequest.ftReceiptCase:X}");
        System.Diagnostics.Activity.Current?.AddTag("possystem.id", receiptRequest.ftPosSystemId);
        System.Diagnostics.Activity.Current?.AddTag("queue.id", _queueId);
        System.Diagnostics.Activity.Current?.AddTag("queue.ReceiptRequest.cbReceiptReference", receiptRequest.cbReceiptReference);
        System.Diagnostics.Activity.Current?.AddTag("queue.ReceiptRequest.cbPreviousReceiptReference", receiptRequest.cbPreviousReceiptReference);
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

        if (queue.CountryCode != "GR")
        {
            // Remove validation once we enable the global validations
            if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Refund) && request.cbPreviousReceiptReference is not null && request.cbPreviousReceiptReference.IsGroup)
            {
                receiptResponse.SetReceiptResponseError("Refunding a receipt is only supported with single references.");
                return (receiptResponse, new List<ftActionJournal>());
            }

            // Remove validation once we enable the global validations
            if (request.IsPartialRefundReceipt() && request.cbPreviousReceiptReference is not null && request.cbPreviousReceiptReference.IsGroup)
            {
                receiptResponse.SetReceiptResponseError("Partial refunding a receipt is only supported with single references.");
                return (receiptResponse, new List<ftActionJournal>());
            }

            // Remove validation once we enable the global validations
            if (request.ftReceiptCase.IsFlag(ReceiptCaseFlags.Void) && request.cbPreviousReceiptReference is not null && request.cbPreviousReceiptReference.IsGroup)
            {
                receiptResponse.SetReceiptResponseError("Voiding a receipt is only supported with single references.");
                return (receiptResponse, new List<ftActionJournal>());
            }

            // Remove validation once we enable the global validations
            var receiptReferences = await _queueStorageProvider.GetReferencedReceiptsAsync(request);
            if (receiptReferences.IsErr)
            {
                receiptResponse.SetReceiptResponseError(receiptReferences.ErrValue!);
                return (receiptResponse, new List<ftActionJournal>());
            }

            if (receiptReferences.OkValue is not null)
            {
                receiptResponse.ftStateData = new MiddlewareStateData
                {
                    PreviousReceiptReference = receiptReferences.OkValue
                };
            }
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
