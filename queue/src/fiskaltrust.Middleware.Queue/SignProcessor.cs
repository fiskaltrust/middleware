using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.PostFiscalization;
using fiskaltrust.Middleware.Queue.Extensions;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.Middleware.Queue.PostFiscalization;
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
        private readonly PostFiscalizationProcessor _postFiscalizationProcessor;

        public SignProcessor(
            ILogger<SignProcessor> logger,
            IConfigurationRepository configurationRepository,
            IMiddlewareQueueItemRepository queueItemRepository,
            IMiddlewareReceiptJournalRepository receiptJournalRepository,
            IMiddlewareActionJournalRepository actionJournalRepository,
            ICryptoHelper cryptoHelper,
            IMarketSpecificSignProcessor countrySpecificSignProcessor,
            MiddlewareConfiguration middlewareConfiguration,
            PostFiscalizationProcessor postFiscalizationProcessor)
        {
            _postFiscalizationProcessor = postFiscalizationProcessor ?? throw new ArgumentNullException(nameof(postFiscalizationProcessor));
            _logger = logger;
            _configurationRepository = configurationRepository ?? throw new ArgumentNullException(nameof(configurationRepository));
            _countrySpecificSignProcessor = countrySpecificSignProcessor;
            _queueItemRepository = queueItemRepository;
            _receiptJournalRepository = receiptJournalRepository;
            _actionJournalRepository = actionJournalRepository;
            _cryptoHelper = cryptoHelper;
            _middlewareConfiguration = middlewareConfiguration;
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
                System.Diagnostics.Activity.Current?.AddTag("queue.ReceiptRequest.cbReceiptReference", request.cbReceiptReference);
                System.Diagnostics.Activity.Current?.AddTag("queue.ReceiptRequest.cbPreviousReceiptReference", request.cbPreviousReceiptReference);
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
                        // RFC 712: a replay returns whatever was persisted for this cbReceiptReference, error state included.
                        // A POS recovering from a failed eInvoicing/eReporting finalize call needs the fiscalized-but-failed
                        // response; on a queue without such services an error response still means the receipt was not
                        // fiscalized, so resending it stays the correct reaction, the response now just carries the reason.
                        return JsonConvert.DeserializeObject<ReceiptResponse>(foundQueueItem.response);
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

            // RFC 712, phase 1 (legacy backport): the configured eInvoicing/eReporting services validate the receipt before
            // any bookkeeping that produces a fiscal record. A rejection or an unreachable service refuses the receipt: no
            // queue item, no fiscal record, no receipt journal, so the POS can correct and resend it as a new transaction.
            var (preflight, v2Request) = await PreflightAsync(data).ConfigureAwait(false);
            if (!preflight.Accepted)
            {
                return await RejectBeforeFiscalizationAsync(queue, data, preflight.Rejection).ConfigureAwait(false);
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

                // RFC 712, phase 3: the services that act on this receipt get the fully fiscalized response, only when
                // fiscalization succeeded. The receipt journal decision below is based on this fiscalization outcome, not
                // on the final ftState: a finalize failure marks a fiscalized receipt with the error state, that receipt
                // is journaled like any other fiscalized receipt, and it is returned rather than thrown (there is no
                // processor exception to rethrow).
                var fiscalizationSucceeded = !receiptResponse.IsError();
                if (fiscalizationSucceeded)
                {
                    receiptResponse = await FinalizeAsync(v2Request, receiptResponse, queueItem, preflight, actionjournals).ConfigureAwait(false);
                }

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

                if (!fiscalizationSucceeded)
                {
                    var errorMessage = "An error occurred during receipt processing, resulting in ftState = 0xEEEE_EEEE.";
                    await CreateActionJournalAsync(errorMessage, $"{receiptResponse.ftState:X}", queueItem.ftQueueItemId).ConfigureAwait(false);

                    // V1 rethrows only a genuine processor exception; when the country processor returned an error
                    // response (ftState EEEE) without throwing, surface it like V2 rather than `throw null` (market-it #635).
                    if (!data.IsV2() && exception != null)
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
                foreach (var actionJournal in actionjournals)
                {
                    await _actionJournalRepository.InsertAsync(actionJournal).ConfigureAwait(false);
                }
                await _countrySpecificSignProcessor.FinalTaskAsync(queue, queueItem, data, _actionJournalRepository, _queueItemRepository, _receiptJournalRepository).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Converts the v1 request to the v2 contract and runs the preflight. A receipt that cannot be converted cannot be
        /// presented to the services, so it is refused before fiscalization like a transport failure.
        /// </summary>
        private async Task<(PostFiscalizationPreflight preflight, ifPOS.v2.ReceiptRequest request)> PreflightAsync(ReceiptRequest data)
        {
            if (!_postFiscalizationProcessor.IsEnabled)
            {
                return (PostFiscalizationPreflight.Disabled, null);
            }

            ifPOS.v2.ReceiptRequest v2Request;
            try
            {
                v2Request = PostFiscalizationMapper.ToV2(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Receipt {ReceiptReference} could not be converted to the v2 contract; it is refused before fiscalization.", data.cbReceiptReference);
                var service = _postFiscalizationProcessor.FirstConfiguredService.Value;
                var rejection = new PostFiscalizationRejection(service, PostFiscalizationServiceOutcome.Failed, $"{service.DisplayName()} validate call failed: the receipt could not be converted to the v2 contract: {ex.Message}", ex.ToString());
                var eInvoicing = service == PostFiscalizationService.EInvoicing ? PostFiscalizationServiceOutcome.Failed : PostFiscalizationServiceOutcome.Disabled;
                var eReporting = service == PostFiscalizationService.EReporting
                    ? PostFiscalizationServiceOutcome.Failed
                    : _postFiscalizationProcessor.IsConfigured(PostFiscalizationService.EReporting) ? PostFiscalizationServiceOutcome.Skipped : PostFiscalizationServiceOutcome.Disabled;
                return (new PostFiscalizationPreflight(eInvoicing, eReporting, rejection), null);
            }

            return (await _postFiscalizationProcessor.ValidateAsync(v2Request).ConfigureAwait(false), v2Request);
        }

        /// <summary>
        /// RFC 712: a configured eInvoicing/eReporting service declined the receipt, or could not be reached, in the preflight.
        /// Nothing is fiscalized and no queue item exists, so the response carries no queue item id and, following the
        /// middleware convention for responses without a queue item, the fail state (0xFFFF_FFFF) rather than the error
        /// state (0xEEEE_EEEE) a processed-but-failed receipt carries. The POS gets that response with a signature naming
        /// the reason, and an action journal entry records the technical detail. Structurally this is one more of the
        /// pre-fiscalization exits of InternalSign; whether rejected receipts should get a queue item after all is an
        /// open decision recorded in the RFC.
        /// </summary>
        private async Task<ReceiptResponse> RejectBeforeFiscalizationAsync(ftQueue queue, ReceiptRequest data, PostFiscalizationRejection rejection)
        {
            var receiptResponse = new ReceiptResponse
            {
                ftCashBoxID = queue.ftCashBoxId.ToString(),
                ftQueueID = queue.ftQueueId.ToString(),
                ftQueueItemID = Guid.Empty.ToString(),
                ftQueueRow = 0,
                cbTerminalID = data.cbTerminalID,
                cbReceiptReference = data.cbReceiptReference,
                ftCashBoxIdentification = await _countrySpecificSignProcessor.GetFtCashBoxIdentificationAsync(queue).ConfigureAwait(false),
                ftReceiptMoment = DateTime.UtcNow,
                ftReceiptIdentification = string.Empty,
                ftSignatures = new SignaturItem[]
                {
                    new SignaturItem
                    {
                        ftSignatureFormat = 0x1,
                        ftSignatureType = PostFiscalizationMapper.LegacyFailureSignatureType(data.ftReceiptCase),
                        Caption = rejection.Caption,
                        Data = rejection.Reason
                    }
                },
                ftState = PostFiscalizationMapper.LegacyFailState(data.ftReceiptCase)
            };
            if (_middlewareConfiguration.IsSandbox)
            {
                receiptResponse.ftSignatures = receiptResponse.ftSignatures.Concat(_signatureFactory.CreateSandboxSignature(_middlewareConfiguration.QueueId)).ToArray();
            }

            var message = $"Receipt \"{data.cbReceiptReference}\" was refused before fiscalization by the {rejection.Service.DisplayName()} service ({rejection.Caption}): {rejection.Reason}";
            _logger.LogWarning(message);
            await _actionJournalRepository.InsertAsync(new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = Guid.Empty,
                Moment = DateTime.UtcNow,
                Priority = 0x10,
                Type = rejection.Caption,
                Message = message,
                DataJson = JsonConvert.SerializeObject(new
                {
                    service = rejection.Service.Key(),
                    phase = "validate",
                    outcome = rejection.Outcome.ToTagValue(),
                    reason = rejection.Reason,
                    detail = rejection.Detail,
                    cbReceiptReference = data.cbReceiptReference,
                    cbTerminalID = data.cbTerminalID,
                    ftReceiptCase = $"0x{data.ftReceiptCase:X}"
                })
            }).ConfigureAwait(false);

            return receiptResponse;
        }

        /// <summary>
        /// Maps the fiscalized v1 response to the v2 contract, runs the finalize phase and merges the result back. A
        /// middleware-side failure in the mapping is handled like a finalize failure: the receipt is fiscalized, so it is
        /// marked as failed and attributed to the first service that applies.
        /// </summary>
        private async Task<ReceiptResponse> FinalizeAsync(ifPOS.v2.ReceiptRequest v2Request, ReceiptResponse receiptResponse, ftQueueItem queueItem, PostFiscalizationPreflight preflight, List<ftActionJournal> actionJournals)
        {
            if (!_postFiscalizationProcessor.IsEnabled || v2Request == null)
            {
                return receiptResponse;
            }

            try
            {
                var v2Response = PostFiscalizationMapper.ToV2(receiptResponse);
                var finalized = await _postFiscalizationProcessor.FinalizeAsync(v2Request, v2Response, queueItem, preflight, actionJournals).ConfigureAwait(false);
                PostFiscalizationMapper.MergeIntoV1(receiptResponse, finalized);
                return receiptResponse;
            }
            catch (Exception ex)
            {
                var service = preflight.EInvoicing == PostFiscalizationServiceOutcome.Applies ? PostFiscalizationService.EInvoicing
                    : preflight.EReporting == PostFiscalizationServiceOutcome.Applies ? PostFiscalizationService.EReporting
                    : _postFiscalizationProcessor.FirstConfiguredService.Value;
                var reason = $"{service.DisplayName()} process call failed after fiscalization: the receipt could not be converted between the v1 and the v2 contract: {ex.Message}";
                _logger.LogError(ex, "Post-fiscalization mapping failed for queue item {QueueItemId}; the receipt is fiscalized and is marked as failed.", queueItem.ftQueueItemId);

                receiptResponse.ftState = PostFiscalizationMapper.LegacyErrorState(receiptResponse.ftState);
                receiptResponse.ftSignatures = (receiptResponse.ftSignatures ?? new SignaturItem[0]).Concat(new[]
                {
                    new SignaturItem
                    {
                        ftSignatureFormat = 0x1,
                        ftSignatureType = PostFiscalizationMapper.LegacyFailureSignatureType(unchecked((long) v2Request.ftReceiptCase)),
                        Caption = service.FailedCaption(),
                        Data = reason
                    }
                }).ToArray();
                actionJournals.Add(new ftActionJournal
                {
                    ftActionJournalId = Guid.NewGuid(),
                    ftQueueId = queueItem.ftQueueId,
                    ftQueueItemId = queueItem.ftQueueItemId,
                    Moment = DateTime.UtcNow,
                    Priority = 0x10,
                    Type = service.FailedCaption(),
                    Message = $"{reason} (queue item {queueItem.ftQueueItemId})",
                    DataJson = JsonConvert.SerializeObject(new { service = service.Key(), phase = "process", reason, detail = ex.ToString() })
                });
                return receiptResponse;
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
