using fiskaltrust.Middleware.SCU.IT.Configuration;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.IT.FiscalizationService;

#nullable enable
public sealed class EpsonSCU /*: ITESSCD */
{
    private readonly EpsonScuConfiguration _configuration;
    private readonly ILogger<EpsonSCU> _logger;

    public EpsonSCU(ILogger<EpsonSCU> logger, EpsonScuConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
}
