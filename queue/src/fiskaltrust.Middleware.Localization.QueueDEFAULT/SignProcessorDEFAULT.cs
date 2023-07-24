using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Localization.QueueDEFAULT.RequestCommands;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT
{
    public class SignProcessorDEFAULT : IMarketSpecificSignProcessor
    {
        private readonly ICountrySpecificSettings _countrySpecificSettings;
        private readonly IRequestCommandFactory _requestCommandFactory;
        private readonly ISSCD _signingDevice;
        private readonly ILogger _logger;

        public SignProcessorDEFAULT(ICountrySpecificSettings countrySpecificSettings, IRequestCommandFactory requestCommandFactory, ISSCD signingDevice, ILogger logger)
        {
            _requestCommandFactory = requestCommandFactory;
            _countrySpecificSettings = countrySpecificSettings;
            _signingDevice = signingDevice;
            _logger = logger;
        }

        public async Task<(ReceiptResponse receiptResponse, List<ftActionJournal> actionJournals)> ProcessAsync(ReceiptRequest request, ftQueue queue, ftQueueItem queueItem)
        {
            var requestCommand = _requestCommandFactory.Create(request);

            if (requestCommand is not ZeroReceiptCommand)
            {
                var requestCommandResponse = await requestCommand.ProcessFailedReceiptRequest(_signingDevice, _logger, _countrySpecificSettings, queue, queueItem, request).ConfigureAwait(false);
                return (requestCommandResponse.ReceiptResponse, requestCommandResponse.ActionJournals.ToList());
            }
        
            var response = await requestCommand.ExecuteAsync(queue, request, queueItem).ConfigureAwait(false);
            return (response.ReceiptResponse, response.ActionJournals.ToList());
        }
    }
}
