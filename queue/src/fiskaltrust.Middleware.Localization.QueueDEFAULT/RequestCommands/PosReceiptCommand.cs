using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands
{
    public struct RefundDetails
    {
        public string Serialnumber { get; set; }
        public long ZRepNumber { get; set; }
        public long ReceiptNumber { get; set; }
        public DateTime ReceiptDateTime { get; set; }
    }

    public class PosReceiptCommand : RequestCommand
    {
        private readonly long _countryBaseState;
        private readonly ICountrySpecificQueueRepository _countrySpecificQueueRepository;
        private readonly ICountrySpecificSettings _countryspecificSettings;
        private readonly IConfigurationRepository _configurationRepository;

        public PosReceiptCommand(IConfigurationRepository configurationRepository, ICountrySpecificSettings countrySpecificSettings)
        {
            _countryspecificSettings = countrySpecificSettings;
            _countrySpecificQueueRepository = countrySpecificSettings.CountrySpecificQueueRepository;
            _countryBaseState = countrySpecificSettings.CountryBaseState;
            _configurationRepository = configurationRepository;
        }
        public override async Task<RequestCommandResponse> ExecuteAsync(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem, bool isBeingResent = false)
        {
            var response = CreateReceiptResponse(queue, request, queueItem, (await _countrySpecificQueueRepository.GetQueueAsync(queue.ftQueueId))
                .CashBoxIdentification, _countryBaseState);
    
            return await Task.FromResult(new RequestCommandResponse
            {
                ReceiptResponse = response,
                Signatures = new List<SignaturItem>(),
                ActionJournals = new List<ftActionJournal>()
            });
        }

        public override Task<bool> ReceiptNeedsReprocessing(ftQueue queue, ReceiptRequest request, ftQueueItem queueItem)
        {
            return Task.FromResult(false);
        }
    }
}
