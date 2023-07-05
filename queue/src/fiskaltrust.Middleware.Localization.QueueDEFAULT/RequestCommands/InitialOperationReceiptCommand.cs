using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.Factories;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;


namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands
{
    public class InitialOperationReceiptCommand : Contracts.RequestCommands.InitialOperationReceiptCommand
    {
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;

        private readonly IConfigurationRepository _configurationRepository;
        private readonly SignatureItemFactoryDEFAULT _signatureItemFactoryDefault;
        private Contracts.RequestCommands.InitialOperationReceiptCommand _initialOperationReceiptCommandImplementation;

        public InitialOperationReceiptCommand(ICountrySpecificSettings countrySpecificQueueSettings, ILogger<InitialOperationReceiptCommand> logger, IConfigurationRepository configurationRepository, SignatureItemFactoryDEFAULT signatureItemFactoryDefault, Contracts.RequestCommands.InitialOperationReceiptCommand initialOperationReceiptCommandImplementation) : base(countrySpecificQueueSettings, logger, configurationRepository)
        {

            _configurationRepository = configurationRepository;
            _signatureItemFactoryDefault = signatureItemFactoryDefault;
            _initialOperationReceiptCommandImplementation = initialOperationReceiptCommandImplementation;
            _countrySpecificQueueRepository = countrySpecificQueueSettings.CountrySpecificQueueRepository;

        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => Task.FromResult(false);
        protected override Task<(ftActionJournal, SignaturItem)> InitializeSCUAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem) => throw new NotImplementedException();
    }
}
