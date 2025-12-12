using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.SCU.ES.VeriFactu;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Soap;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;

#pragma warning disable
[Collection("VeriFactu Tests")]
public class VeriFactuAcceptanceTests : ESScuAcceptanceTestBase
{
    private readonly VeriFactuSCUConfiguration _configuration;

    public VeriFactuAcceptanceTests()
    {

    }

    protected override IESSSCD CreateScu()
    {
        var requestHandler = new HttpClientHandler();
        requestHandler.ClientCertificates.Add(_configuration.Certificate);
        
        var httpClient = new HttpClient(requestHandler)
        {
            BaseAddress = new Uri(_configuration.BaseUrl)
        };
        httpClient.DefaultRequestHeaders.Add("AcceptCharset", "utf-8");
        
        var client = new Client(httpClient);
        return new VeriFactuSCU(client, _configuration);
    }
}
