using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Clients;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Requests;
using fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Models.Responses;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.UnitTest
{
    internal interface HttpResponseMessageMembers
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }

    public class DeSerialization
    {
        [Fact]
        public async void Test1()
        {
            var httpMessageHandler = Mock.Of<HttpMessageHandler>(MockBehavior.Strict);
            Mock.Get(httpMessageHandler)
                        .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.Content.ReadAsStringAsync().Result.Contains("<getinfo")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        """
                        <?xml version="1.0" encoding="utf-8"?>
                        <infoResp success="true" status="0">
                            <serialNumber>STMTE770207</serialNumber>
                            <iscalized>SI</iscalized>
                            <fpuState>IN SERVIZIO</fpuState>
                            <hwInitExhausted>NO</hwInitExhausted>
                            <hwInitNumber>55</hwInitNumber>
                            <fmPresent>SI</fmPresent>
                            <mfExhausted>NO</mfExhausted>
                            <zSetNumber>142</zSetNumber>
                            <ejPresent>SI</ejPresent>
                            <ejFull>NO</ejFull>
                            <ejFilling>0.394%</ejFilling>
                            <simulation>NO</simulation>
                            <demoMode>NO</demoMode>
                            <vatSplit>NO</vatSplit>
                            <privatekey>SI</privatekey>
                            <certificate>SI</certificate>
                            <certValidFrom>26/02/2019</certValidFrom>
                            <certValidTo>26/02/2022</certValidTo>
                            <certExpired>NO</certExpired>
                            <dateProg>21/12/2024</dateProg>
                            <minWaste>1</minWaste>
                            <maxWaste>3</maxWaste>
                            <delaysNum>5</delaysNum>
                            <advancesNum>10</advancesNum>
                            <timeSync>NO</timeSync>
                            <vatNumberDealer>02498250345</vatNumberDealer>
                            <pointOfSaleNum>1</pointOfSaleNum>
                            <vatNumberRetailer>02498250345</vatNumberRetailer>
                            <retailerDescription>CUSTOM</retailerDescription>
                            <retailerPostalCode>1111</retailerPostalCode>
                        </infoResp>
                        """
                    )
                });

            var client = new CustomRTPrinterClient(new HttpClient(httpMessageHandler) { BaseAddress = new("http://localhost") });

            var response = await client.PostAsync<GetInfo, InfoResp>();

            response.SerialNumber.Should().Be("STMTE770207");
            response.DateProg.Should().Be(new DateTime(2024, 12, 21));
        }
    }
}