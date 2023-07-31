using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueES.Externals.ifpos;
using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Services;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueES.RequestCommands
{
    public class DailyClosingReceiptCommand : Contracts.RequestCommands.DailyClosingReceiptCommand
    {
        private readonly long _countryBaseState;
        protected override long CountryBaseState => _countryBaseState;
        private readonly ICountrySpecificSettings _countryspecificSettings;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly SignatureItemFactoryES _signatureItemFactory;
        private readonly IESSSCD _client;
        private readonly ISSCD _signingDevice;
        private readonly ILogger<DailyClosingReceiptCommand> _logger;

        public DailyClosingReceiptCommand(ISSCD signingDevice, ILogger<DailyClosingReceiptCommand> logger, SignatureItemFactoryES signatureItemFactory, ICountrySpecificSettings countrySpecificSettings, IESSSCDProvider sscdProvider)
        {
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _client = sscdProvider.Instance;
            _signatureItemFactory = signatureItemFactory;
            _countryspecificSettings = countrySpecificSettings;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
            _signingDevice = signingDevice;
            _logger = logger;
        }

        protected override async Task<string> GetCashboxIdentificationAsync(Guid ftQueueId)
        {
            var queue = await _countrySpecificQueueRepository.GetQueueAsync(ftQueueId).ConfigureAwait(false);
            return queue.CashBoxIdentification;
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);

        protected override async Task<RequestCommandResponse> SpecializeAsync(RequestCommandResponse requestCommandResponse, ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            return await Task.FromResult(requestCommandResponse);
        }
    }
}
