using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1.it;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueueIT.Services
{
    public class SigningDeviceIT : ISSCD
    {
        private readonly IITSSCD _client;
        private readonly ILogger _logger;


        public SigningDeviceIT(IITSSCDProvider itIsscdProvider, ILogger<SigningDeviceIT> logger)
        {
            _client = itIsscdProvider.Instance;
            _logger = logger;
        }

        public async Task<bool> IsSSCDAvailable()
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
