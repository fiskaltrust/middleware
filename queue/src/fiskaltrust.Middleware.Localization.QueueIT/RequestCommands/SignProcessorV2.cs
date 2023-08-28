using System;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using System.Collections.Generic;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class SignProcessorV2
    {
        private readonly ICountrySpecificSettings _countrySpecificSettings;
        protected readonly IConfigurationRepository _configurationRepository;
        private readonly IJournalITRepository _journalITRepository;
        private readonly ISSCD _signingDevice;
        private readonly ILogger<SignProcessorIT> _logger;

        public SignProcessorV2(ISSCD signingDevice, ILogger<SignProcessorIT> logger, ICountrySpecificSettings countrySpecificSettings, IConfigurationRepository configurationRepository, IJournalITRepository journalITRepository)
        {
            _configurationRepository = configurationRepository;
            _journalITRepository = journalITRepository;
            _countrySpecificSettings = countrySpecificSettings;
            _signingDevice = signingDevice;
            _logger = logger;
        }


        public async Task<RequestCommandResponse> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            IReceiptTypeProcessor receiptTypeProcessor = null;
            var queueIT = await _configurationRepository.GetQueueITAsync(queue.ftQueueId).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIT.CashBoxIdentification, _countrySpecificSettings.CountryBaseState);
            try
            {
                if (receiptTypeProcessor.GenerateJournalIT)
                {
                    if (receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ReceiptNumber)) != null)
                    {
                        var journalIT = new ftJournalIT().FromResponse(queueIT, queueItem, new ScuResponse()
                        {
                            ftReceiptCase = request.ftReceiptCase,
                            ReceiptDateTime = DateTime.Parse(receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ReceiptTimestamp)).Data),
                            ReceiptNumber = long.Parse(receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ReceiptNumber)).Data),
                            ZRepNumber = long.Parse(receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ZNumber)).Data)
                        });
                        await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
                    }
                }
                return new RequestCommandResponse
                {
                    ReceiptResponse = receiptResponse,
                    ActionJournals = new List<ftActionJournal>()
                };
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

        public async Task<RequestCommandResponse> ProcessFailedReceiptRequest(ftQueueIT queueIt, ftQueueItem queueItem, ReceiptResponse receiptResponse)
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
            return new RequestCommandResponse { ReceiptResponse = receiptResponse };
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
