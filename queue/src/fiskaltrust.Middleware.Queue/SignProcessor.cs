using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Queue.Extensions;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


namespace fiskaltrust.Middleware.Queue
{
    public class SignProcessor : ISignProcessor
    {
        private readonly IMarketSpecificSignProcessor _countrySpecificSignProcessor;
        private readonly ILogger<SignProcessor> _logger;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;
        private readonly IMiddlewareReceiptJournalRepository _receiptJournalRepository;
        private readonly IMiddlewareActionJournalRepository _actionJournalRepository;
        private readonly ICryptoHelper _cryptoHelper;

        private readonly MiddlewareConfiguration _middlewareConfiguration;
        private readonly SignatureFactory _signatureFactory;
        //private readonly Action<string> _onMessage;

        public SignProcessor(
            ILogger<SignProcessor> logger,
            IConfigurationRepository configurationRepository,
            IMiddlewareQueueItemRepository queueItemRepository,
            IMiddlewareReceiptJournalRepository receiptJournalRepository,
            IMiddlewareActionJournalRepository actionJournalRepository,
            ICryptoHelper cryptoHelper,
            IMarketSpecificSignProcessor countrySpecificSignProcessor,
            MiddlewareConfiguration middlewareConfiguration)
        {
            _logger = logger;
            _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
            _countrySpecificSignProcessor = countrySpecificSignProcessor;
            _queueItemRepository = queueItemRepository;
            _receiptJournalRepository = receiptJournalRepository;
            _actionJournalRepository = actionJournalRepository;
            _cryptoHelper = cryptoHelper;
            _middlewareConfiguration = middlewareConfiguration;
            //_onMessage = configuration.OnMessage;
            _signatureFactory = new SignatureFactory();
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
                if (dataCashBoxId != _middlewareConfiguration.CashBoxId)
                {
                    throw new Exception("Provided CashBoxId does not match current CashBoxId");
                }

                var queue = await _configurationRepository.GetQueueAsync(_middlewareConfiguration.QueueId).ConfigureAwait(false);

                var response = await InternalSign(queue, request).ConfigureAwait(false);

#if NET6_0_OR_GREATER
                if (response != null)
                {
                    System.Diagnostics.Activity.Current?.AddTag("queue.ReceiptResponse.ftState", $"0x{response.ftState:X}");
                }
                else
                {
                    System.Diagnostics.Activity.Current?.AddTag("queue.ReceiptResponse.ftState", $"null");
                }
                System.Diagnostics.Activity.Current?.AddTag("queue.ReceiptRequest.ftReceiptCase", $"0x{request.ftReceiptCase:X}");
                System.Diagnostics.Activity.Current?.AddTag("queue.id", queue.ftQueueId);
#endif

                return response;
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
                        var message = $"Queue {_middlewareConfiguration.QueueId} found cbReceiptReference \"{foundQueueItem.cbReceiptReference}\"";
                        _logger.LogWarning(message);
                        await CreateActionJournalAsync(message, "", foundQueueItem.ftQueueItemId).ConfigureAwait(false);
                        var response = JsonConvert.DeserializeObject<ReceiptResponse>(foundQueueItem.response);
                        if ((response.ftState & 0xFFFF_FFFF) == 0xEEEE_EEEE && (data.ftReceiptCase & 0xF000_0000_0000) != 0x2000_0000_0000)
                        {
                            return null;
                        }
                        else
                        { 
                            return response;
                        }                                
                    }
                }
                catch (Exception x)
                {
                    var message = $"Queue {_middlewareConfiguration.QueueId} problem on receitrequest";
                    _logger.LogError(x, message);
                    await CreateActionJournalAsync(message, "", null).ConfigureAwait(false);
                }


                if (_middlewareConfiguration.ReceiptRequestMode == 1)
                {
                    //try to sign, remove receiptrequest-flag
                    data.ftReceiptCase -= 0x0000800000000000L;
                }
                else
                {
                    return null;
                }
            }

            await _countrySpecificSignProcessor.FirstTaskAsync().ConfigureAwait(false);

            var queueItem = new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueMoment = DateTime.UtcNow,
                ftQueueTimeout = queue.Timeout,
                cbReceiptMoment = data.cbReceiptMoment,
                cbTerminalID = data.cbTerminalID,
                cbReceiptReference = data.cbReceiptReference,
                ftQueueRow = ++queue.ftQueuedRow,
                ProcessingVersion = _middlewareConfiguration.ProcessingVersion
            };
            if (queueItem.ftQueueTimeout == 0)
            {
                queueItem.ftQueueTimeout = 15000;
            }

            queueItem.country = ReceiptRequestHelper.GetCountry(data);
            queueItem.version = ReceiptRequestHelper.GetRequestVersion(data);
            queueItem.request = JsonConvert.SerializeObject(data);
            queueItem.requestHash = _cryptoHelper.GenerateBase64Hash(queueItem.request);
            _logger.LogTrace("SignProcessor.InternalSign: Adding QueueItem to database.");
            await _queueItemRepository.InsertOrUpdateAsync(queueItem).ConfigureAwait(false);
            _logger.LogTrace("SignProcessor.InternalSign: Updating Queue in database.");
            await _configurationRepository.InsertOrUpdateQueueAsync(queue).ConfigureAwait(false);

            var actionjournals = new List<ftActionJournal>();
            ftReceiptJournal receiptJournal = null;
            try
            {
                queueItem.ftWorkMoment = DateTime.UtcNow;
                _logger.LogTrace("SignProcessor.InternalSign: Calling country specific SignProcessor.");
                ReceiptResponse receiptResponse;
                List<ftActionJournal> countrySpecificActionJournals;
                Exception exception = null;
                try
                {
                    (receiptResponse, countrySpecificActionJournals) = await _countrySpecificSignProcessor.ProcessAsync(data, queue, queueItem).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    exception = e;
                    countrySpecificActionJournals = new();
                    receiptResponse = new ReceiptResponse
                    {
                        ftCashBoxID = queue.ftCashBoxId.ToString(),
                        ftQueueID = queue.ftQueueId.ToString(),
                        ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                        ftQueueRow = queue.ftCurrentRow,
                        cbTerminalID = data.cbTerminalID,
                        cbReceiptReference = data.cbReceiptReference,
                        ftCashBoxIdentification = await _countrySpecificSignProcessor.GetFtCashBoxIdentificationAsync(queue),
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

                if (_middlewareConfiguration.IsSandbox)
                {
                    receiptResponse.ftSignatures = receiptResponse.ftSignatures.Concat(_signatureFactory.CreateSandboxSignature(_middlewareConfiguration.QueueId));
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

                    if ((data.ftReceiptCase & 0xF000_0000_0000) != 0x2000_0000_0000)
                    {
                        throw exception;
                    }
                    // TODO: This state indicates that something went wrong while processing the receipt request.
                    //       While we will probably introduce a parameter for this we are right now just returning
                    //       the receipt response as it is.
                    //       Another thing that needs to be considered is if and when we put things into the security
                    //       mechanism. Since there might be cases where we still need to store it though.
                    return receiptResponse;
                }
                else
                {
                    _logger.LogTrace("SignProcessor.InternalSign: Adding ReceiptJournal to database.");
                    receiptJournal = await CreateReceiptJournalAsync(queue, queueItem, data).ConfigureAwait(false);
                }
                return receiptResponse;
            }
            finally
            {
                OnMessage(queueItem, receiptJournal);
                foreach (var actionJournal in actionjournals)
                {
                    await _actionJournalRepository.InsertAsync(actionJournal).ConfigureAwait(false);
                }
                await _countrySpecificSignProcessor.FinalTaskAsync(queue, queueItem, data, _actionJournalRepository, _queueItemRepository, _receiptJournalRepository).ConfigureAwait(false);
            }
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private void OnMessage(ftQueueItem queueItem, ftReceiptJournal receiptJournal)
        {
            // Temporary uncomment this, as it's throwing an exception when used after 5 minutes due to the runtime service object being disposed
            //try
            //{
            //    _onMessage?.Invoke(JsonConvert.SerializeObject(new ReceiptProcessedMessage
            //    {
            //        QueueItem = queueItem,
            //        ReceiptJournal = receiptJournal
            //    }));
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "An error occurred while executing the OnMessage event. This is most likely caused by an exception in another component.");
            //}
        }
#pragma warning restore IDE0060 // Remove unused parameter

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
                ftQueueId = _middlewareConfiguration.QueueId,
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
    }
}
