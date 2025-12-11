using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using fiskaltrust.Middleware.SCU.ES.TicketBAIAraba;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;
#pragma warning disable 
[Collection("TicketBAI Araba Tests")]
public class TicketBAIArabaAcceptanceTests : ESScuAcceptanceTestBase
{
    private readonly TicketBaiSCUConfiguration _configuration;

    public TicketBAIArabaAcceptanceTests()
    {

    }

    protected override IESSSCD CreateScu()
    {
        var logger = NullLogger<TicketBaiSCU>.Instance;
        var territory = new TicketBaiArabaTerritory();
        return new TicketBaiArabaSCU(logger, _configuration);
    }
}
