using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.Configuration;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.ES.TBAI;

public sealed class TbaiSCU //: IESSSCD 
{
    private readonly TBaiScuConfiguration _configuration;
    private readonly ILogger<TbaiSCU> _logger;

    public TbaiSCU(ILogger<TbaiSCU> logger, TBaiScuConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
}
