using System;
using System.Threading.Tasks;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.RequestCommands;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.RequestCommands.Factories;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Localization.QueueIT.Services;
using fiskaltrust.ifPOS.v1.it;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT.RequestCommands
{
    public class ZeroReceiptCommandIT : ZeroReceiptCommand
    {
        private readonly IITSSCD _client;
        private readonly ILogger _logger;


        public ZeroReceiptCommandIT(IITSSCDProvider itIsscdProvider, ICountrySpecificSettings countryspecificSettings, IMiddlewareQueueItemRepository queueItemRepository, IRequestCommandFactory requestCommandFactory, ILogger<RequestCommand> logger, IActionJournalRepository actionJournalRepository) :
            base(countryspecificSettings, queueItemRepository, requestCommandFactory, logger, actionJournalRepository)
        {
            _client = itIsscdProvider.Instance;
            _logger = logger;
        }

        public override async Task<bool> IsSigningDeviceAvailable()
        {
            try
            {
                var deviceInfo = await _client.GetDeviceInfoAsync().ConfigureAwait(false);
                _logger.LogInformation(JsonConvert.SerializeObject(deviceInfo));
                return true;
            }catch (Exception ex) {
                _logger.LogError(ex, "Error on DeviceInfo Request.");
                return false;
            }
        }
    }
}
