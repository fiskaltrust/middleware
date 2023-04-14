using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI;

public sealed class TicketBaiSCU //: IESSSCD 
{
#pragma warning disable IDE0052
    private readonly TicketBaiSCUConfiguration _configuration;
    private readonly ILogger<TicketBaiSCU> _logger;

    public TicketBaiSCU(ILogger<TicketBaiSCU> logger, TicketBaiSCUConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
}
