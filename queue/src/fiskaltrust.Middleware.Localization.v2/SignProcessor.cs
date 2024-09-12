using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.v2
{
    public class SignProcessor : ISignProcessor
    {
        private readonly ILogger<SignProcessor> _logger;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;
        private readonly IMiddlewareReceiptJournalRepository _receiptJournalRepository;
        private readonly IMiddlewareActionJournalRepository _actionJournalRepository;
        private readonly ICryptoHelper _cryptoHelper;
        private readonly Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)>> _processRequest;
        private readonly string _cashBoxIdentification;
        private readonly Guid _queueId = Guid.Empty;
        private readonly Guid _cashBoxId = Guid.Empty;
        private readonly bool _isSandbox;
        private readonly int _receiptRequestMode = 0;

        //  ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem);

        public SignProcessor(
            ILogger<SignProcessor> logger,
            IConfigurationRepository configurationRepository,
            IMiddlewareQueueItemRepository queueItemRepository,
            IMiddlewareReceiptJournalRepository receiptJournalRepository,
            IMiddlewareActionJournalRepository actionJournalRepository,
            ICryptoHelper cryptoHelper,
            Func<ReceiptRequest, ReceiptResponse, ftQueue, ftQueueItem, Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)>> processRequest,
            string cashBoxIdentification,
            MiddlewareConfiguration configuration)
        {
            _logger = logger;
            _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
            _queueItemRepository = queueItemRepository;
            _receiptJournalRepository = receiptJournalRepository;
            _actionJournalRepository = actionJournalRepository;
            _cryptoHelper = cryptoHelper;
            _processRequest = processRequest;
            _cashBoxIdentification = cashBoxIdentification;
            _queueId = configuration.QueueId;
            _cashBoxId = configuration.CashBoxId;
            _isSandbox = configuration.IsSandbox;
            _receiptRequestMode = configuration.ReceiptRequestMode;
        }

        public async Task<ReceiptResponse> ProcessAsync(ReceiptRequest request)
        {
            _logger.LogTrace("SignProcessor.ProcessAsync called.");
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }
                if (!Guid.TryParse(request.ftCashBoxID, out var dataCashBoxId))
                {
                    throw new InvalidCastException($"Cannot parse CashBoxId {request.ftCashBoxID}");
                }
                if (dataCashBoxId != _cashBoxId)
                {
                    throw new Exception("Provided CashBoxId does not match current CashBoxId");
                }

                var queue = await _configurationRepository.GetQueueAsync(_queueId).ConfigureAwait(false);

                return await InternalSign(queue, request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
                throw;
            }
        }

        private async Task<ReceiptResponse> InternalSign(ftQueue queue, ReceiptRequest data)
        {
            _logger.LogTrace("SignProcessor.InternalSign called.");
            if ((data.ftReceiptCase & 0x0000800000000000L) > 0)
            {
                try
                {
                    var foundQueueItem = await GetExistingQueueItemOrNullAsync(data).ConfigureAwait(false);
                    if (foundQueueItem != null)
                    {
                        var message = $"Queue {_queueId} found cbReceiptReference \"{foundQueueItem.cbReceiptReference}\"";
                        _logger.LogWarning(message);
                        await CreateActionJournalAsync(message, "", foundQueueItem.ftQueueItemId).ConfigureAwait(false);
                        return JsonConvert.DeserializeObject<ReceiptResponse>(foundQueueItem.response);
                    }
                }
                catch (Exception x)
                {
                    var message = $"Queue {_queueId} problem on receitrequest";
                    _logger.LogError(x, message);
                    await CreateActionJournalAsync(message, "", null).ConfigureAwait(false);
                }


                if (_receiptRequestMode == 1)
                {
                    //try to sign, remove receiptrequest-flag
                    data.ftReceiptCase -= 0x0000800000000000L;
                }
                else
                {
                    return null;
                }
            }

            var queueItem = new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueMoment = DateTime.UtcNow,
                ftQueueTimeout = queue.Timeout,
                cbReceiptMoment = data.cbReceiptMoment,
                cbTerminalID = data.cbTerminalID,
                cbReceiptReference = data.cbReceiptReference,
                ftQueueRow = ++queue.ftQueuedRow
            };
            if (queueItem.ftQueueTimeout == 0)
            {
                queueItem.ftQueueTimeout = 15000;
            }

            queueItem.country = data.GetCountry();
            queueItem.version = "v0"; // Todo .. get version from request
            queueItem.request = JsonConvert.SerializeObject(data);
            queueItem.requestHash = _cryptoHelper.GenerateBase64Hash(queueItem.request);
            _logger.LogTrace("SignProcessor.InternalSign: Adding QueueItem to database.");
            await _queueItemRepository.InsertOrUpdateAsync(queueItem).ConfigureAwait(false);
            _logger.LogTrace("SignProcessor.InternalSign: Updating Queue in database.");
            await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);

            var actionjournals = new List<ftActionJournal>();
            try
            {
                queueItem.ftWorkMoment = DateTime.UtcNow;
                _logger.LogTrace("SignProcessor.InternalSign: Calling country specific SignProcessor.");
                ReceiptResponse receiptResponse;
                List<ftActionJournal> countrySpecificActionJournals;
                try
                {
                    (receiptResponse, countrySpecificActionJournals) = await ProcessAsync(data, queue, queueItem).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    countrySpecificActionJournals = new();
                    receiptResponse = new ReceiptResponse
                    {
                        ftCashBoxID = queue.ftCashBoxId.ToString(),
                        ftQueueID = queue.ftQueueId.ToString(),
                        ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                        ftQueueRow = queue.ftCurrentRow,
                        cbTerminalID = data.cbTerminalID,
                        cbReceiptReference = data.cbReceiptReference,
                        ftCashBoxIdentification = _cashBoxIdentification,
                        ftReceiptMoment = DateTime.UtcNow,
                        ftSignatures = new SignaturItem[] {
                            new SignaturItem() {
                                ftSignatureFormat = 0x1,
                                ftSignatureType = (long) (((ulong) data.ftReceiptCase & 0xFFFF_0000_0000_0000) | 0x2000_0000_3000),
                                Caption = "uncaught-exeption",
                                Data = e.ToString()
                            }
                        },
                        ftState = (long) (((ulong) data.ftReceiptCase & 0xFFFF_0000_0000_0000) | 0x2000_EEEE_EEEE)
                    };
                }
                _logger.LogTrace("SignProcessor.InternalSign: Country specific SignProcessor finished.");

                actionjournals.AddRange(countrySpecificActionJournals);

                if (_isSandbox)
                {
                    receiptResponse.ftSignatures = receiptResponse.ftSignatures.Concat(new List<SignaturItem> { SignatureFactory.CreateSandboxSignature(_queueId) }).ToArray();
                }

                queueItem.response = JsonConvert.SerializeObject(receiptResponse);
                queueItem.responseHash = _cryptoHelper.GenerateBase64Hash(queueItem.response);
                queueItem.ftDoneMoment = DateTime.UtcNow;
                queue.ftCurrentRow++;

                _logger.LogTrace("SignProcessor.InternalSign: Updating QueueItem in database.");
                await _queueItemRepository.InsertOrUpdateAsync(queueItem).ConfigureAwait(false);
                _logger.LogTrace("SignProcessor.InternalSign: Updating Queue in database.");
                await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);

                if ((receiptResponse.ftState & 0xFFFF_FFFF) == 0xEEEE_EEEE)
                {
                    var errorMessage = "An error occurred during receipt processing, resulting in ftState = 0xEEEE_EEEE.";
                    await CreateActionJournalAsync(errorMessage, $"{receiptResponse.ftState:X}", queueItem.ftQueueItemId).ConfigureAwait(false);
                    return receiptResponse;
                }
                else
                {
                    _logger.LogTrace("SignProcessor.InternalSign: Adding ReceiptJournal to database.");
                    _ = await CreateReceiptJournalAsync(queue, queueItem, data).ConfigureAwait(false);
                }
                return receiptResponse;
            }
            finally
            {
                foreach (var actionJournal in actionjournals)
                {
                    await _actionJournalRepository.InsertAsync(actionJournal).ConfigureAwait(false);
                }
            }
        }

        private async Task<ftQueueItem> GetExistingQueueItemOrNullAsync(ReceiptRequest data)
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
            if (itemRequest.cbChargeItems.Length == data.cbChargeItems.Length && itemRequest.cbPayItems.Length == data.cbPayItems.Length)
            {
                for (var i = 0; i < itemRequest.cbChargeItems.Length; i++)
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
                for (var i = 0; i < itemRequest.cbPayItems.Length; i++)
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

        public async Task<ftReceiptJournal> CreateReceiptJournalAsync(ftQueue queue, ftQueueItem queueItem, ReceiptRequest receiptrequest)
        {
            queue.ftReceiptNumerator++;
            var receiptjournal = new ftReceiptJournal
            {
                ftReceiptJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = queueItem.ftQueueItemId,
                ftReceiptMoment = DateTime.UtcNow,
                ftReceiptNumber = queue.ftReceiptNumerator
            };
            if (receiptrequest.cbReceiptAmount.HasValue)
            {
                receiptjournal.ftReceiptTotal = receiptrequest.cbReceiptAmount.Value;
            }
            else
            {
                receiptjournal.ftReceiptTotal = (receiptrequest?.cbChargeItems?.Sum(ci => ci.Amount)).GetValueOrDefault();
            }
            receiptjournal.ftReceiptHash = _cryptoHelper.GenerateBase64ChainHash(queue.ftReceiptHash, receiptjournal, queueItem);
            await _receiptJournalRepository.InsertAsync(receiptjournal).ConfigureAwait(false);
            await UpdateQueuesLastReceipt(queue, receiptjournal).ConfigureAwait(false);
            return receiptjournal;
        }

        private async Task UpdateQueuesLastReceipt(ftQueue queue, ftReceiptJournal receiptJournal)
        {
            queue.ftReceiptHash = receiptJournal.ftReceiptHash;
            queue.ftReceiptTotalizer += receiptJournal.ftReceiptTotal;
            await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var receiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            var receiptResponse = new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (long) ((ulong) request.ftReceiptCase & 0xFFFF_0000_0000_0000),
                ftReceiptIdentification = receiptIdentification
            };
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
            var actionJournals = new List<ftActionJournal>
            {
                new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueItem.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    Message = $"QueueId {queueItem.ftQueueId} has not been activated yet."
                }
            };
            receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
            receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return (receiptResponse, actionJournals);
        }

        private (ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals) ReturnWithQueueIsDisabled(ftQueue queue, ReceiptResponse receiptResponse, ftQueueItem queueItem)
        {
            var actionJournals = new List<ftActionJournal>
            {
                new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueItem.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    Message = $"QueueId {queueItem.ftQueueId} has been disabled."
                }
            };
            receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
            receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return (receiptResponse, actionJournals);
        }
    }
}