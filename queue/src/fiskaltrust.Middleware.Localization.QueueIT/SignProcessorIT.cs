using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Constants;
using System;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueIT.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.DailyOperations;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueIT.RequestCommands.v2.Lifecycle;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class SignProcessorIT : IMarketSpecificSignProcessor
    {
        private readonly ICountrySpecificSettings _countrySpecificSettings;
        protected readonly IConfigurationRepository _configurationRepository;
        private readonly IJournalITRepository _journalITRepository;
        private readonly ReceiptTypeProcessor _receiptTypeProcessor;
        private readonly ISSCD _signingDevice;
        private readonly ILogger<SignProcessorIT> _logger;
        private bool _loggedDisabledQueueReceiptRequest;

        public SignProcessorIT(ISSCD signingDevice, ILogger<SignProcessorIT> logger, ICountrySpecificSettings countrySpecificSettings, IConfigurationRepository configurationRepository, IJournalITRepository journalITRepository, ReceiptTypeProcessor receiptTypeProcessor)
        {
            _configurationRepository = configurationRepository;
            _journalITRepository = journalITRepository;
            _receiptTypeProcessor = receiptTypeProcessor;
            _countrySpecificSettings = countrySpecificSettings;
            _signingDevice = signingDevice;
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
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIT.CashBoxIdentification, _countrySpecificSettings.CountryBaseState);

            if ((queue.IsNew() || queue.IsDeactivated()) && receiptTypeProcessor is not InitialOperationReceipt0x4001)
            {
                return await ReturnWithQueueIsDisabled(queue, queueIT, request, queueItem);
            }

            if (queueIT.SSCDFailCount > 0 && receiptTypeProcessor is not ZeroReceipt0x200)
            {
                (var response, var actionJournals) = await ProcessFailedReceiptRequest(queueIT, queueItem, receiptResponse).ConfigureAwait(false);
                return (response, actionJournals);
            }

            try
            {
                (var response, var actionJournals)= await receiptTypeProcessor.ExecuteAsync(queue, queueIT, request, receiptResponse, queueItem).ConfigureAwait(false);
                if (receiptTypeProcessor.GenerateJournalIT)
                {
                    if (response.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ReceiptNumber)) != null)
                    {
                        // TBD insert daily closing
                        //var journalIT = new ftJournalIT().FromResponse(queueIt, queueItem, new ScuResponse()
                        //{
                        //    ftReceiptCase = request.ftReceiptCase,
                        //    ZRepNumber = zNumber
                        //});
                        //await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
                        var journalIT = new ftJournalIT().FromResponse(queueIT, queueItem, new ScuResponse()
                        {
                            ftReceiptCase = request.ftReceiptCase,
                            ReceiptDateTime = DateTime.Parse(response.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ReceiptTimestamp)).Data),
                            ReceiptNumber = long.Parse(response.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ReceiptNumber)).Data),
                            ZRepNumber = long.Parse(response.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ZNumber)).Data)
                        });
                        await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
                    }
                }
                return (response, actionJournals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process request");
                if (receiptTypeProcessor.FailureModeAllowed)
                {
                    return await ProcessFailedReceiptRequest(queueIT, queueItem, receiptResponse);
                }
                // TBD => set errorstate because we arent' able to proceed with this
                throw;
            }
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ReturnWithQueueIsDisabled(ftQueue queue, ftQueueIT queueIT, ReceiptRequest request, ftQueueItem queueItem)
        {
            var receiptResponse = CreateReceiptResponse(request, queueItem, queueIT);
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
                            Message = $"QueueId {queueItem.ftQueueId} was not activated or already deactivated"
                        }
                    );
                _loggedDisabledQueueReceiptRequest = true;
            }

            receiptResponse.ftState += ftStatesFlags.SECURITY_MECHAMISN_DEACTIVATED;
            receiptResponse.ftReceiptIdentification = $"ft{queue.ftReceiptNumerator:X}#";
            return await Task.FromResult((receiptResponse, actionJournals)).ConfigureAwait(false);
        }

        protected static ReceiptResponse CreateReceiptResponse(ReceiptRequest request, ftQueueItem queueItem, ftQueueIT queueIT)
        {
            return new ReceiptResponse
            {
                ftCashBoxID = request.ftCashBoxID,
                ftCashBoxIdentification = queueIT.CashBoxIdentification,
                ftQueueID = queueItem.ftQueueId.ToString(),
                ftQueueItemID = queueItem.ftQueueItemId.ToString(),
                ftQueueRow = queueItem.ftQueueRow,
                cbTerminalID = request.cbTerminalID,
                cbReceiptReference = request.cbReceiptReference,
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0x4954000000000000
            };
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessFailedReceiptRequest(ftQueueIT queueIt, ftQueueItem queueItem, ReceiptResponse receiptResponse)
        {
            if (queueIt.SSCDFailCount == 0)
            {
                queueIt.SSCDFailMoment = DateTime.UtcNow;
                queueIt.SSCDFailQueueItemId = queueItem.ftQueueItemId;
            }
            queueIt.SSCDFailCount++;
            await _countrySpecificSettings.CountrySpecificQueueRepository.InsertOrUpdateQueueAsync(queueIt).ConfigureAwait(false);
            var log = $"Queue is in failed mode. SSCDFailMoment: {queueIt.SSCDFailMoment}, SSCDFailCount: {queueIt.SSCDFailCount}.";
            receiptResponse.ftState |= 0x2;
            log += " When connection is established use zeroreceipt for subsequent booking!";
            var signingAvail = await _signingDevice.IsSSCDAvailable().ConfigureAwait(false);
            log += signingAvail ? " Signing device is available." : " Signing device is not available.";
            _logger.LogInformation(log);
            receiptResponse.SetFtStateData(new StateDetail() { FailedReceiptCount = queueIt.SSCDFailCount, FailMoment = queueIt.SSCDFailMoment, SigningDeviceAvailable = signingAvail });
            return (receiptResponse, new List<ftActionJournal>());
        }

        protected ReceiptResponse CreateReceiptResponse(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, string ftCashBoxIdentification, long ftState)
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
