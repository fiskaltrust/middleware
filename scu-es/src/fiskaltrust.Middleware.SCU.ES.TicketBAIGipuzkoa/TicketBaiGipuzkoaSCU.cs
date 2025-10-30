using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.ES.TicketBAIGipuzkoa;

public class TicketBAIGipuzkoaSCU : TicketBaiSCU
{
    public TicketBAIGipuzkoaSCU(ILogger<TicketBAIGipuzkoaSCU> logger, TicketBaiSCUConfiguration configuration) : base(logger, configuration, new TicketBaiGipuzkoaTerritory())
    {
    }
}