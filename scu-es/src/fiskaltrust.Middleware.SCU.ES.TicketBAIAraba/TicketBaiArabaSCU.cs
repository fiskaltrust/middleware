using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;

#pragma warning disable IDE0052

namespace fiskaltrust.Middleware.SCU.ES.TicketBAIAraba;

public class TicketBaiArabaSCU : TicketBaiSCU
{
    public TicketBaiArabaSCU(ILogger<TicketBaiSCU> logger, TicketBaiSCUConfiguration configuration) : base(logger, configuration, new TicketBaiArabaTerritory())
    {
    }
}