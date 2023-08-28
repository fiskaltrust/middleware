using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.ifPOS.v1.it;
using System.Linq;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Constants;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class PosReceiptCommand : RequestCommand
    {
        private readonly long _countryBaseState;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly ICountrySpecificSettings _countryspecificSettings;
        private readonly IMiddlewareJournalITRepository _journalITRepository;
        private readonly IITSSCD _client;
        private readonly ISSCD _signingDevice;
        private readonly ILogger<DailyClosingReceiptCommand> _logger;

        public PosReceiptCommand(ISSCD signingDevice, ILogger<DailyClosingReceiptCommand> logger, IITSSCDProvider itIsscdProvider, IMiddlewareJournalITRepository journalITRepository, ICountrySpecificSettings countrySpecificSettings)
        {
            _client = itIsscdProvider.Instance;
            _journalITRepository = journalITRepository;
            _countryspecificSettings = countrySpecificSettings;
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
            _signingDevice = signingDevice;
            _logger = logger;
        }

        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isBeingResent = false)
        {
            var queueIt = await _countrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId).ConfigureAwait(false);
            var receiptResponse = CreateReceiptResponse(queue, request, queueItem, queueIt.CashBoxIdentification, _countryBaseState);
            try
            {
                var result = await _client.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request,
                    ReceiptResponse = receiptResponse,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to Process request at SCU level.");
                return await ProcessFailedReceiptRequest(_signingDevice, _logger, _countryspecificSettings, queue, queueItem, request).ConfigureAwait(false);
            }

            if (receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ReceiptNumber)) != null)
            {
                var journalIT = new ftJournalIT().FromResponse(queueIt, queueItem, new ScuResponse()
                {
                    ftReceiptCase = request.ftReceiptCase,
                    ReceiptDateTime = DateTime.Parse(receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ReceiptTimestamp)).Data),
                    ReceiptNumber = long.Parse(receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ReceiptNumber)).Data),
                    ZRepNumber = long.Parse(receiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ZNumber)).Data)
                });
                await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
            }
            return new RequestCommandResponse
            {
                ReceiptResponse = receiptResponse,
                ActionJournals = new List<ftActionJournal>()
            };
        }

        public override async Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            var journalIt = await _journalITRepository.GetByQueueItemId(queueItem.ftQueueItemId).ConfigureAwait(false);
            return journalIt == null;
        }
    }
}
