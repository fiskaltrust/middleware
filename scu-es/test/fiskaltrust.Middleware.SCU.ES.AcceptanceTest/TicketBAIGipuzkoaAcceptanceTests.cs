using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using fiskaltrust.Middleware.SCU.ES.TicketBAIGipuzkoa;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;
#pragma warning disable 
[Collection("TicketBAI Gipuzkoa Tests")]
public class TicketBAIGipuzkoaAcceptanceTests : ESScuAcceptanceTestBase
{
    private readonly TicketBaiSCUConfiguration _configuration;

    public TicketBAIGipuzkoaAcceptanceTests()
    {

    }

    protected override IESSSCD CreateScu()
    {
        var logger = NullLogger<TicketBaiSCU>.Instance;
        var territory = new TicketBaiGipuzkoaTerritory();
        return new TicketBaiGipuzkoaSCU(logger, _configuration);
    }
}
