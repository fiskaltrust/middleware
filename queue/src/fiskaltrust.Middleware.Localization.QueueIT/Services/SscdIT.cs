using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using fiskaltrust.Middleware.Contracts.Interfaces;

namespace fiskaltrust.Middleware.Localization.QueueIT.Services
{
    public class SscdIT : ISSCD
    {
        private readonly IITSSCDProvider _itIsscdProvider;
        private readonly ILogger _logger;


        public SscdIT(IITSSCDProvider itIsscdProvider, ILogger<SscdIT> logger)
        {
            _itIsscdProvider = itIsscdProvider;
            _logger = logger;
        }

        public async Task<bool> IsSSCDAvailable()
        {
            try
            {
                var deviceInfo = await _itIsscdProvider.GetRTInfoAsync().ConfigureAwait(false);
                _logger.LogDebug(JsonConvert.SerializeObject(deviceInfo));
                return true;
            }catch (Exception ex) {
                _logger.LogError(ex, "Error on DeviceInfo Request.");
                return false;
            }
        }
    }
}
