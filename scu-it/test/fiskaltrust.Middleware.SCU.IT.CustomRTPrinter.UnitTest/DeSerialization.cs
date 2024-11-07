
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
    public class DeSerialization
    {
        [Fact]
        public async void Test1()
        {
            var httpMessageHandler = Mock.Of<HttpMessageHandler>(MockBehavior.Strict);
            Mock.Get(httpMessageHandler)
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.Content.ReadAsStringAsync().Result.Contains("<printerCommand><getInfo")), ItExpr.IsAny<CancellationToken>())
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

            var response = await client.SendCommand<InfoResp>(new GetInfo());

            response.Success.Should().BeTrue();
            response.Status.Should().Be(0);
            response.SerialNumber.Should().Be("STMTE770207");
            //response.DateProg.Should().Be(new DateTime(2024, 12, 21));
        }

        [Fact]
        public async void Test2()
        {
            var httpMessageHandler = Mock.Of<HttpMessageHandler>(MockBehavior.Strict);
            Mock.Get(httpMessageHandler)
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(m => m.Content.ReadAsStringAsync().Result.Contains("<printerCommand><getInfo")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        """
                        <?xml version="1.0" encoding="utf-8"?>
                        <response success="true" status= "1">
                            <addInfo>
                                <elementList>Tag1 value</elementList>
                                <lastIdCmd IdCmd=""> Tag2 value </lastIdCmd>
                                <responseBuf>Tag3 value</responseBuf >
                                <lastCommand>Tag4 value</lastCommand>
                                <dateTime>21/12/2112</dateTime>
                                <printerStatus>0000</printerStatus>
                                <fpStatus>123</fpStatus>
                                <receiptStep>Tag8 value </receiptStep>
                                <nClose>Tag9 value</nClose>
                                <iscalDoc>Tag10 value</iscalDoc >
                                <notFiscalDoc>Tag11 value</notFiscalDoc>
                            </addInfo>
                        </response>
                        """)
                });

            var client = new CustomRTPrinterClient(new HttpClient(httpMessageHandler) { BaseAddress = new("http://localhost") });

            var response = await client.SendCommand<Response<string>>(new GetInfo());

            response.Success.Should().BeTrue();
            response.Status.Should().Be(1);

            response.AddInfo.ResponseBuf.Should().Be("Tag3 value");
            response.AddInfo.FpStatus.Should().Be(123);
        }

        public void Test3()
        {
            var request = new PrinterFiscalReceipt(
                new IFiscalRecord[] {
                    new PrintRecItem {
                        Description = "Test",
                        Quantity = 1,
                        UnitPrice = 3.3m,
                        Department = 2,
                        IdVat = 1,
                    },
                    new PrintRecItem {
                        Description = "Test33",
                        Quantity = 1,
                        UnitPrice = 3.3m,
                        Department = 2,
                        IdVat = 1,
                    },
                    new PrintRecItemVoid {
                        Description = "Test",
                        Quantity = 1,
                        UnitPrice = 3.3m,
                        Department = 2,
                        IdVat = 1,
                    },
                    new PrintRecTotal {
                        Description = "Testp",
                        Payment = 3.3m,
                        PaymentType = 1,
                    },
                }
            );

            var serialized = CustomRTPrinterClient.Serialize(request);
            serialized.Should().Be(
                """
                <printerFiscalReceipt>
                    <beginFiscalReceipt />
                    <printRecItem description="Test" unitPrice="3.3" department="2" idVat="1" quantity="1" />
                    <printRecItem description="Test33" unitPrice="3.3" department="2" idVat="1" quantity="1" />
                    <printRecItemVoid description="Test" unitPrice="3.3" department="2" idVat="1" quantity="1" />
                    <printRecTotal description="Testp" payment="3.3" paymentType="1" paymentQty="0" />
                    <endFiscalReceipt />
                </printerFiscalReceipt>
                """.Replace("\r", "").Replace("\n", "").Replace("    ", "")
            );
        }
    }
}