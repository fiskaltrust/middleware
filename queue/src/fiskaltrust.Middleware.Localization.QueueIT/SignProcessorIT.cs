using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using fiskaltrust.Middleware.Contracts.Extensions;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.v2.DailyOperations;
using fiskaltrust.Middleware.Localization.QueueIT.v2.Lifecycle;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.Middleware.Contracts.Repositories;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1.it;

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
        private bool _loggedDisabledQueueReceiptRequest;

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
            if (!queueIT.ftSignaturCreationUnitITId.HasValue && !queue.IsActive())
            {
                throw new NullReferenceException(nameof(queueIT.ftSignaturCreationUnitITId));
            }

            var receiptTypeProcessor = _receiptTypeProcessor.Create(request);
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIT.CashBoxIdentification, Cases.BASE_STATE);

            if (queue.IsDeactivated())
            {
                return await ReturnWithQueueIsDisabled(queue, queueIT, request, queueItem);
            }

            if (receiptTypeProcessor is InitialOperationReceipt0x4001)
            {
                if (!queue.IsNew())
                {
                    throw new Exception("The queue is already operational. It is not allowed to send another InitOperation Receipt");
                }

                (var response, var actionJournals) = await receiptTypeProcessor.ExecuteAsync(queue, queueIT, request, receiptResponse, queueItem).ConfigureAwait(false);
                return (response, actionJournals);
            }

            if (queue.IsNew())
            {
                return await ReturnWithQueueIsNotActive(queue, queueIT, request, queueItem);
            }

            if (queueIT.SSCDFailCount > 0 && receiptTypeProcessor is not ZeroReceipt0x200)
            {
                (var response, var actionJournals) = await ProcessFailedReceiptRequest(queueIT, queueItem, receiptResponse).ConfigureAwait(false);
                return (response, actionJournals);
            }

            if (request.IsVoid() || request.IsRefund())
            {
                var queueItems = _queueItemRepository.GetByReceiptReferenceAsync(request.cbPreviousReceiptReference, request.cbTerminalID);
                // What should we do in this case? Cannot really proceed with the storno but we
                await foreach (var existingQueueItem in queueItems)
                {
                    var referencedResponse = JsonConvert.DeserializeObject<ReceiptResponse>(queueItem.response);
                    var documentNumber = referencedResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTDocumentNumber)).Data;
                    var zNumber = referencedResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTZNumber)).Data;
                    var signatures = new List<SignaturItem>();
                    signatures.AddRange(receiptResponse.ftSignatures);
                    signatures.AddRange(new List<SignaturItem>
                    {
                        new SignaturItem
                        {
                            Caption = "<reference-z-number>",
                            Data = zNumber.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = 0x4954000000000000 | (long) SignatureTypesIT.RTReferenceZNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-doc-number>",
                            Data = documentNumber.ToString(),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = 0x4954000000000000 | (long) SignatureTypesIT.RTReferenceDocumentNumber
                        },
                        new SignaturItem
                        {
                            Caption = "<reference-timestamp>",
                            Data = queueItem.cbReceiptMoment.ToString("yyyy-MM-dd HH:mm:ss"),
                            ftSignatureFormat = (long) SignaturItem.Formats.Text,
                            ftSignatureType = 0x4954000000000000 | (long) SignatureTypesIT.RTDocumentMoment
                        },
                    });
                    receiptResponse.ftSignatures = signatures.ToArray();
                    break;
                }
            }

            try
            {
                (var response, var actionJournals) = await receiptTypeProcessor.ExecuteAsync(queue, queueIT, request, receiptResponse, queueItem).ConfigureAwait(false);
                if (response.ftSignatures.Any(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTDocumentNumber)) &&
                         response.ftSignatures.Any(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTZNumber)))
                {
                    var documentNumber = response.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTDocumentNumber)).Data;
                    var zNumber = response.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTZNumber)).Data;
                    var journalIT = ftJournalITFactory.CreateFrom(queueItem, queueIT, new ScuResponse()
                    {
                        ftReceiptCase = request.ftReceiptCase,
                        ReceiptNumber = long.Parse(documentNumber),
                        ZRepNumber = long.Parse(zNumber)
                    });
                    await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
                }
                else if (response.ftSignatures.Any(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTZNumber)))
                {
                    var zNumber = response.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 | (long) SignatureTypesIT.RTZNumber)).Data;
                    var journalIT = ftJournalITFactory.CreateFrom(queueItem, queueIT, new ScuResponse()
                    {
                        ftReceiptCase = request.ftReceiptCase,
                        ZRepNumber = long.Parse(zNumber)
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

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ReturnWithQueueIsNotActive(ftQueue queue, ftQueueIT queueIT, ReceiptRequest request, ftQueueItem queueItem)
        {
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIT.CashBoxIdentification, Cases.BASE_STATE);
            var actionJournals = new List<ftActionJournal>();
            if (!_loggedDisabledQueueReceiptRequest)
            {
                actionJournals.Add(
                        new ftActionJournal
                        {
                            ftActionJournalId = Guid.NewGuid(),
                            ftQueueId = queueItem.ftQueueId,
                            ftQueueItemId = queueItem.ftQueueItemId,
                            Moment = DateTime.UtcNow,
                            Message = $"QueueId {queueItem.ftQueueId} is not activated yet."
                        }
                    );
                _loggedDisabledQueueReceiptRequest = true;
            }

            receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
            receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return await Task.FromResult((receiptResponse, actionJournals)).ConfigureAwait(false);
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ReturnWithQueueIsDisabled(ftQueue queue, ftQueueIT queueIT, ReceiptRequest request, ftQueueItem queueItem)
        {
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIT.CashBoxIdentification, Cases.BASE_STATE);
            var actionJournals = new List<ftActionJournal>();
            if (!_loggedDisabledQueueReceiptRequest)
            {
                actionJournals.Add(
                        new ftActionJournal
                        {
                            ftActionJournalId = Guid.NewGuid(),
                            ftQueueId = queueItem.ftQueueId,
                            ftQueueItemId = queueItem.ftQueueItemId,
                            Moment = DateTime.UtcNow,
                            Message = $"QueueId {queueItem.ftQueueId} has been disabled."
                        }
                    );
                _loggedDisabledQueueReceiptRequest = true;
            }

            receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
            receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return await Task.FromResult((receiptResponse, actionJournals)).ConfigureAwait(false);
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

        private ReceiptResponse CreateReceiptResponse(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, string ftCashBoxIdentification, long ftState)
        {
            var receiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = ftState,
                ftReceiptIdentification = receiptIdentification,
                ftCashBoxIdentification = ftCashBoxIdentification
            };
        }
    }
}
