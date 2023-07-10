using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.errors;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueIT.Extensions;
using fiskaltrust.Middleware.Localization.QueueIT.Factories;
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
        private readonly SignatureItemFactoryIT _signatureItemFactoryIT;
        private readonly IMiddlewareJournalITRepository _journalITRepository;
        private readonly IITSSCD _client;
        private readonly ISigningDevice _signingDevice;
        private readonly ILogger<DailyClosingReceiptCommand> _logger;

        public DailyClosingReceiptCommand(ISigningDevice signingDevice, ILogger<DailyClosingReceiptCommand> logger, SignatureItemFactoryIT signatureItemFactoryIT, ICountrySpecificSettings countrySpecificSettings, IITSSCDProvider itIsscdProvider, IMiddlewareJournalITRepository journalITRepository)
        {
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _client = itIsscdProvider.Instance;
            _journalITRepository = journalITRepository;
            _signatureItemFactoryIT = signatureItemFactoryIT;
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
            var response = await _client.ExecuteDailyClosingAsync(new DailyClosingRequest() ).ConfigureAwait(false);

            if (!response.Success)
            {
                if (response.SSCDErrorInfo.Type == SSCDErrorType.Connection)
                {
                    return await ProcessFailedReceiptRequest(_signingDevice, _logger, _countryspecificSettings, queue, queueItem, request).ConfigureAwait(false);
                }
                else
                {
                    throw new SSCDErrorException(response.SSCDErrorInfo.Type, response.SSCDErrorInfo.Info);
                }
            }
            else
            {
                requestCommandResponse.ReceiptResponse.ftReceiptIdentification += $"Z{response.ZRepNumber}";
                requestCommandResponse.ReceiptResponse.ftSignatures = _signatureItemFactoryIT.CreatePosReceiptSignatures(response);
                var queueIt = await _countrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId).ConfigureAwait(false);
                var journalIT = new ftJournalIT().FromResponse(queueIt, queueItem, new ScuResponse()
                {
                    DataJson = response.ReportDataJson,
                    ftReceiptCase = request.ftReceiptCase,
                    ZRepNumber = response.ZRepNumber
                });
                await _journalITRepository.InsertAsync(journalIT).ConfigureAwait(false);
                return requestCommandResponse;
            }
        }
    }
}
