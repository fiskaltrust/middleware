using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using fiskaltrust.ifPOS.v1.it;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueueIT.Services
{
    public class SscdIT : ISSCD
    {
        private readonly IITSSCD _client;
        private readonly ILogger _logger;


        public SscdIT(IITSSCDProvider itIsscdProvider, ILogger<SscdIT> logger)
        {
            _client = itIsscdProvider.Instance;
            _logger = logger;
        }

        public async Task<bool> IsSSCDAvailable()
        {
            try
            {
                var deviceInfo = await _client.GetDeviceInfoAsync().ConfigureAwait(false);
                _logger.LogDebug(JsonConvert.SerializeObject(deviceInfo));
                return true;
            }catch (Exception ex) {
                _logger.LogError(ex, "Error on DeviceInfo Request.");
                return false;
            }
        }
    }
}
