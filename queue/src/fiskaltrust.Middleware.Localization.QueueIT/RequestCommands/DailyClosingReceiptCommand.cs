using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class DailyClosingReceiptCommand : Contracts.RequestCommands.DailyClosingReceiptCommand
    {
        private readonly long _countryBaseState;
        protected override long CountryBaseState => _countryBaseState;
        private readonly ICountrySpecificSettings _countryspecificSettings;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly IMiddlewareJournalITRepository _journalITRepository;
        private readonly IITSSCDProvider _itIsscdProvider;
        private readonly ISSCD _signingDevice;
        private readonly ILogger<DailyClosingReceiptCommand> _logger;

        public DailyClosingReceiptCommand(ISSCD signingDevice, ILogger<DailyClosingReceiptCommand> logger, ICountrySpecificSettings countrySpecificSettings, IITSSCDProvider itIsscdProvider, IMiddlewareJournalITRepository journalITRepository)
        {
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _itIsscdProvider = itIsscdProvider;
            _journalITRepository = journalITRepository;
            _countryspecificSettings = countrySpecificSettings;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
            _signingDevice = signingDevice;
            _logger = logger;
        }

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId)
        {
            var queueIt = await _countrySpecificQueueRepository.GetQueueAsync(ftQueueId).ConfigureAwait(false);
            return queueIt.CashBoxIdentification;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);

        protected override async Task<RequestCommandResponse> SpecializeAsync(RequestCommandResponse requestCommandResponse, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            try
            {
                var result = await _itIsscdProvider.ProcessReceiptAsync(new ProcessRequest
                {
                    ReceiptRequest = request,
                    ReceiptResponse = requestCommandResponse.ReceiptResponse,
                });
                var zNumber = long.Parse(result.ReceiptResponse.ftSignatures.FirstOrDefault(x => x.ftSignatureType == (0x4954000000000000 & (long) SignatureTypesIT.ZNumber)).Data);
                requestCommandResponse.ReceiptResponse.ftReceiptIdentification += $"Z{zNumber}";
                var queueIt = await _countrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId).ConfigureAwait(false);
                var journalIT = new ftJournalIT().FromResponse(queueIt, queueItem, new ScuResponse()
                {
                    ftReceiptCase = request.ftReceiptCase,
                    ZRepNumber = zNumber
                });
                await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
                return requestCommandResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to Process request at SCU level.");
                return await ProcessFailedReceiptRequest(_signingDevice, _logger, _countryspecificSettings, queue, queueItem, request).ConfigureAwait(false);
            }
        }
    }
}
