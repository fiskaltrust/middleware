using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Communication;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloud
{
    public sealed class SwissbitCloudSCU : DeutscheFiskalSCU
    {
        public SwissbitCloudSCU(ILogger<DeutscheFiskalSCU> logger, DeutscheFiskalSCUConfiguration configuration, IFccInitializationService fccInitializer,
            IFccProcessHost fccProcessHost, IFccDownloadService fccDownloader, FccErsApiProvider fccErsApiProvider, FccAdminApiProvider fccAdminApiProvider) : 
            base(logger, configuration, fccInitializer, fccProcessHost, fccDownloader, fccErsApiProvider, fccAdminApiProvider) { }
    }
}
