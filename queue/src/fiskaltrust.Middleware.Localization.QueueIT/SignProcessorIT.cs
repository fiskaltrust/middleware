using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.v2.DailyOperations;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Lifecycle;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.Middleware.Contracts.Repositories;
using Newtonsoft.Json;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignProcessorIT : IMarketSpecificSignProcessor
    {
        protected readonly IConfigurationRepository _configurationRepository;
        private readonly IJournalITRepository _journalITRepository;
        private readonly ReceiptTypeProcessorFactory _receiptTypeProcessor;
        private readonly IMiddlewareQueueItemRepository _queueItemRepository;
        private readonly IITSSCDProvider _itSSCDProvider;
        private readonly ILogger<SignProcessorIT> _logger;

        public SignProcessorIT(IITSSCDProvider itSSCDProvider, ILogger<SignProcessorIT> logger, IConfigurationRepository configurationRepository, IJournalITRepository journalITRepository, ReceiptTypeProcessorFactory receiptTypeProcessor, IMiddlewareQueueItemRepository queueItemRepository)
        {
            _configurationRepository = configurationRepository;
            _journalITRepository = journalITRepository;
            _receiptTypeProcessor = receiptTypeProcessor;
            _queueItemRepository = queueItemRepository;
            _itSSCDProvider = itSSCDProvider;
            _logger = logger;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var queueIT = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
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
                ftState = Cases.BASE_STATE,
                ftReceiptIdentification = receiptIdentification,
                ftCashBoxIdentification = queueIT.CashBoxIdentification
            };

            var receiptTypeProcessor = _receiptTypeProcessor.Create(request);
            if (receiptTypeProcessor == null)
            {
                receiptResponse.SetReceiptResponseErrored(string.Format("The given ReceiptCase 0x{0:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.", request.ftReceiptCase));
                return (receiptResponse, new List<ftActionJournal>());
            }

            if (queue.IsDeactivated())
            {
                return ReturnWithQueueIsDisabled(queue, receiptResponse, queueItem);
            }

            if (receiptTypeProcessor is InitialOperationReceipt0x4001)
            {
                if (!queue.IsNew())
                {
                    receiptResponse.SetReceiptResponseErrored("The queue is already operational. It is not allowed to send another InitOperation Receipt");
                    return (receiptResponse, new List<ftActionJournal>());
                }

                (var response, var actionJournals) = await receiptTypeProcessor.ExecuteAsync(queue, queueIT, request, receiptResponse, queueItem).ConfigureAwait(false);
                return (response, actionJournals);
            }

            if (queue.IsNew())
            {
                return ReturnWithQueueIsNotActive(queue, receiptResponse, queueItem);
            }

            if (receiptTypeProcessor is ZeroReceipt0x200)
            {
                try
                {
                    (var response, var actionJournals) = await receiptTypeProcessor.ExecuteAsync(queue, queueIT, request, receiptResponse, queueItem).ConfigureAwait(false);
                    return (response, actionJournals);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process ZeroReceipt.");
                    receiptResponse.SetReceiptResponseErrored("Failed to process ZeroReceipt with the following exception message: " + ex.Message);
                    return (receiptResponse, new List<ftActionJournal>());
                }
            }

            if (queueIT.SSCDFailCount > 0 && receiptTypeProcessor is not ZeroReceipt0x200)
            {
                (var response, var actionJournals) = await ProcessFailedReceiptRequest(queueIT, queueItem, receiptResponse).ConfigureAwait(false);
                return (response, actionJournals);
            }

            if (request.IsVoid() || request.IsRefund())
            {
                await LoadReceiptReferencesToResponse(request, queueItem, receiptResponse);
            }

            try
            {
                (var response, var actionJournals) = await receiptTypeProcessor.ExecuteAsync(queue, queueIT, request, receiptResponse, queueItem).ConfigureAwait(false);
                if (response.HasFailed())
                {
                    return (response, actionJournals);
                }

                var documentNumber = response.GetSignaturItem(SignatureTypesIT.RTDocumentNumber);
                var zNumber = response.GetSignaturItem(SignatureTypesIT.RTZNumber);
                if (documentNumber != null && zNumber != null)
                {
                    response.InsertSignatureItems(SignaturBuilder.CreatePOSReceiptFormatSignatures(response));
                    var journalIT = ftJournalITFactory.CreateFrom(queueItem, queueIT, new ScuResponse()
                    {
                        ftReceiptCase = request.ftReceiptCase,
                        ReceiptNumber = long.Parse(documentNumber.Data),
                        ZRepNumber = long.Parse(zNumber.Data)
                    });
                    await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
                }
                else if (zNumber != null)
                {
                    var journalIT = ftJournalITFactory.CreateFrom(queueItem, queueIT, new ScuResponse()
                    {
                        ftReceiptCase = request.ftReceiptCase,
                        ZRepNumber = long.Parse(zNumber.Data)
                    });
                    await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
                }
                return (response, actionJournals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process request");
                return await ProcessFailedReceiptRequest(queueIT, queueItem, receiptResponse);
            }
        }

        private async Task LoadReceiptReferencesToResponse(ReceiptRequest request, ftQueueItem queueItem, ReceiptResponse receiptResponse)
        {
            var queueItems = _queueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference, request.cbTerminalID);
            // What should we do in this case? Cannot really proceed with the storno but we
            await foreach (var existingQueueItem in queueItems)
            {
                var referencedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(existingQueueItem.response);
                var documentNumber = referencedResponse.GetSignaturItem(SignatureTypesIT.RTDocumentNumber).Data;
                var zNumber = referencedResponse.GetSignaturItem(SignatureTypesIT.RTZNumber).Data;
                var signatures = new List<SignaturItem>();
                signatures.AddRange(receiptResponse.ftSignatures);
                signatures.AddRange(new List<SignaturItem>
                    {
                        new SignaturItem
                        {
                            Caption = "<reference-z-number>",
                            Data = zNumber.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceZNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-doc-number>",
                            Data = documentNumber.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTReferenceDocumentNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-timestamp>",
                            Data = queueItem.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = Cases.BASE_STATE | (long) SignatureTypesIT.RTDocumentMoment
                        },
                    });
                receiptResponse.ftSignatures = signatures.ToArray();
                break;
            }
        }

        public (ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals) ReturnWithQueueIsNotActive(ftQueue queue, ReceiptResponse receiptResponse, ftQueueItem queueItem)
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

        public (ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals) ReturnWithQueueIsDisabled(ftQueue queue, ReceiptResponse receiptResponse, ftQueueItem queueItem)
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

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessFailedReceiptRequest(ftQueueIT queueIt, ftQueueItem queueItem, ReceiptResponse receiptResponse)
        {
            if (queueIt.SSCDFailCount == 0)
            {
                queueIt.SSCDFailMoment = DateTime.UtcNow;
                queueIt.SSCDFailQueueItemId = queueItem.ftQueueItemId;
            }
            queueIt.SSCDFailCount++;
            await _configurationRepository.InsertOrUpdateQueueITAsync(queueIt).ConfigureAwait(false);
            var log = $"Queue is in failed mode. SSCDFailMoment: {queueIt.SSCDFailMoment}, SSCDFailCount: {queueIt.SSCDFailCount}.";
            receiptResponse.ftState |= 0x2;
            log += " When connection is established use zeroreceipt for subsequent booking!";
            var signingAvail = await _itSSCDProvider.IsSSCDAvailable().ConfigureAwait(false);
            log += signingAvail ? " Signing device is available." : " Signing device is not available.";
            _logger.LogInformation(log);
            receiptResponse.SetFtStateData(new StateDetail() { FailedReceiptCount = queueIt.SSCDFailCount, FailMoment = queueIt.SSCDFailMoment, SigningDeviceAvailable = signingAvail });
            return (receiptResponse, new List<ftActionJournal>());
        }
    }
}
