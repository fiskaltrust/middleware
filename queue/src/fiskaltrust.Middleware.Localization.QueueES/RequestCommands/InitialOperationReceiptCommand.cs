using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Repositories;

using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Localization.QueueES.Factories;
using fiskaltrust.Middleware.Localization.QueueES.Services;
using fiskaltrust.Middleware.Localization.QueueES.Externals.ifpos;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueES.RequestCommands
{
    public class InitialOperationReceiptCommand : Contracts.RequestCommands.InitialOperationReceiptCommand
    {
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;

        private readonly IConfigurationRepository _configurationRepository;
        private readonly SignatureItemFactoryES _signatureItemFactory;
        private readonly IESSSCD _client;

        public InitialOperationReceiptCommand(ICountrySpecificSettings countrySpecificQueueSettings, IESSSCDProvider sscdProvider, ILogger<InitialOperationReceiptCommand> logger, IConfigurationRepository configurationRepository, SignatureItemFactoryES signatureItemFactory) : base(countrySpecificQueueSettings, logger, configurationRepository)
        {
            _client = sscdProvider.Instance;
            _configurationRepository = configurationRepository;
            _signatureItemFactory = signatureItemFactory;
            _countrySpecificQueueRepository = countrySpecificQueueSettings.CountrySpecificQueueRepository;
        }

        protected override async Task<(ftActionJournal, SignaturItem)> InitializeSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => throw new NotImplementedException();

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => throw new NotImplementedException();
    }
}
