using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using fiskaltrust.Middleware.SCU.ES.TicketBAIBizkaia;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;

#pragma warning disable 
[Collection("TicketBAI Bizkaia Tests")]
public class TicketBAIBizkaiaAcceptanceTests : ESScuAcceptanceTestBase
{
    private readonly TicketBaiSCUConfiguration _configuration;

    public TicketBAIBizkaiaAcceptanceTests()
    {
        

    }

    protected override IESSSCD CreateScu()
    {
        var logger = NullLogger<TicketBaiSCU>. Instance;
        var territory = new TicketBaiBizkaiaTerritory();
        return new TicketBaiBizkaiaSCU(logger, _configuration);
    }
}
