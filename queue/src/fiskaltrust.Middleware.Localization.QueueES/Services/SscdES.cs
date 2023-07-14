using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Localization.QueueES.Externals.ifpos;

#pragma warning disable

namespace fiskaltrust.Middleware.Localization.QueueES.Services
{
    public class SscdES : ISSCD
    {
        private readonly IESSSCD _client;
        private readonly ILogger _logger;


        public SscdES(IESSSCDProvider itIsscdProvider, ILogger<SscdES> logger)
        {
            _client = itIsscdProvider.Instance;
            _logger = logger;
        }

        public async Task<bool> IsSSCDAvailable()
        {
            return true;
        }
    }
}
