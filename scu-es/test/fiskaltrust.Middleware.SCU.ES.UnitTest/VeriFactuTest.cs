using System.Security.Cryptography.X509Certificates;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.ES.Models;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System.IO;
using System;
using fiskaltrust.Middleware.SCU.ES.VeriFactu;
using fiskaltrust.Middleware.SCU.ES.Soap;
using System.Net.Http;
using System.Linq;

namespace fiskaltrust.Middleware.SCU.ES.UnitTest
{
    public class VeriFactuTest()
    {
        [Fact, Trait("only", "local")]
        public async Task ResetReceipts()
        {
            var veriFactuSCUConfiguration = new VeriFactuSCUConfiguration
            {
                Nif = "M0291081Q",
                NombreRazonEmisor = "Thomas Steininger",
                Certificate = new X509Certificate2("TestCertificates/VeriFactu/Certificado_RPJ_A39200019_CERTIFICADO_ENTIDAD_PRUEBAS_4_Pre.pfx", "1234")
            };

            var receiptRequest = ExampleCashSales(Guid.NewGuid());

            var receiptResponse = new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 0,
                ftCashBoxIdentification = Guid.NewGuid().ToString(),
                ftReceiptIdentification = $"0#0/{receiptRequest.cbReceiptReference}",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State) 0x4752_2000_0000_0000,
            };

            var veriFactuMapping = new VeriFactuMapping(veriFactuSCUConfiguration);
            var journalES = veriFactuMapping.CreateRegistroFacturacionAlta(receiptRequest, receiptResponse, null, null);

            var requestHandler = new HttpClientHandler();
            requestHandler.ClientCertificates.Add(veriFactuSCUConfiguration.Certificate);
            var httpClient = new HttpClient(requestHandler)
            {
                BaseAddress = new Uri(veriFactuSCUConfiguration.BaseUrl),
            };
            httpClient.DefaultRequestHeaders.Add("AcceptCharset", "utf-8");
            var client = new Client(httpClient);

            var envelope = new Envelope<RequestBody>
            {
                Body = new RequestBody
                {
                    RegFactuSistemaFacturacion = veriFactuMapping.CreateRegFactuSistemaFacturacion(journalES)
                }
            };
            envelope.Body.RegFactuSistemaFacturacion.RegistroFactura.First().Item.As<RegistroFacturacionAlta>().Subsanacion = Booleano.S;
            envelope.Body.RegFactuSistemaFacturacion.RegistroFactura.First().Item.As<RegistroFacturacionAlta>().RechazoPrevio = RechazoPrevio.X;

            var requestXml = envelope.XmlSerialize();
            var response = await client.SendAsync(envelope);

            using var scope = new AssertionScope();
            response.IsOk.Should().BeTrue($"Response should not be error:\n{response.ErrValue?.ToString()}\n");
            response.OkValue!.RespuestaLinea.Should().HaveCount(1);
            response.OkValue!.RespuestaLinea!.First().CodigoErrorRegistro.Should().BeNullOrEmpty();
            response.OkValue!.RespuestaLinea!.First().DescripcionErrorRegistro.Should().BeNullOrEmpty();
        }

        [Fact, Trait("only", "local")]
        public async Task VeriFactuTestInit()
        {
            var veriFactuSCUConfiguration = new VeriFactuSCUConfiguration
            {
                Nif = "M0291081Q",
                NombreRazonEmisor = "Thomas Steininger",
                Certificate = new X509Certificate2("TestCertificates/VeriFactu/Certificado_RPJ_A39200019_CERTIFICADO_ENTIDAD_PRUEBAS_4_Pre.pfx", "1234")
            };

            var receiptRequest = ExampleCashSales(Guid.NewGuid());
            var receiptResponse = new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 0,
                ftCashBoxIdentification = Guid.NewGuid().ToString(),
                ftReceiptIdentification = $"0#0/{receiptRequest.cbReceiptReference}",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State) 0x4752_2000_0000_0000,
            };

            var veriFactuMapping = new VeriFactuMapping(veriFactuSCUConfiguration);
            var journalES = veriFactuMapping.CreateRegistroFacturacionAlta(receiptRequest, receiptResponse, null, null);

            var requestHandler = new HttpClientHandler();
            requestHandler.ClientCertificates.Add(veriFactuSCUConfiguration.Certificate);
            var httpClient = new HttpClient(requestHandler)
            {
                BaseAddress = new Uri(veriFactuSCUConfiguration.BaseUrl),
            };
            httpClient.DefaultRequestHeaders.Add("AcceptCharset", "utf-8");
            var client = new Client(httpClient);

            var envelope = new Envelope<RequestBody>
            {
                Body = new RequestBody
                {
                    RegFactuSistemaFacturacion = veriFactuMapping.CreateRegFactuSistemaFacturacion(journalES)
                }
            };
            var requestXml = envelope.XmlSerialize();
            var response = await client.SendAsync(envelope);

            using var scope = new AssertionScope();
            response.IsOk.Should().BeTrue($"Response should not be error\n{response.ErrValue?.ToString()}\n");
            response.OkValue!.RespuestaLinea.Should().HaveCount(1);
            response.OkValue!.RespuestaLinea!.First().CodigoErrorRegistro.Should().BeNullOrEmpty();
            response.OkValue!.RespuestaLinea!.First().DescripcionErrorRegistro.Should().BeNullOrEmpty();
        }

        private static ReceiptRequest ExampleCashSales(Guid cashBoxId)
        {
            return new ReceiptRequest
            {
                ftCashBoxID = cashBoxId,
                ftReceiptCase = (ReceiptCase) 0x4752_2000_0000_0000,
                cbTerminalID = "1",
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbReceiptMoment = DateTime.UtcNow,
                cbChargeItems =
                            [
                                new ChargeItem
                    {
                        Position = 1,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = 1.30m,
                        Amount = 6.2m,
                        VATRate = 21m,
                        Quantity = 1,
                        Description = "ChargeItem1"
                    },
                    new ChargeItem
                    {
                        Position = 2,
                        ftChargeItemCase = (ChargeItemCase) 0x4752_2000_0000_0013,
                        VATAmount = 1.30m,
                        Amount = 6.2m,
                        VATRate = 21m,
                        Quantity = 1,
                        Description = "ChargeItem2"
                    }
                            ],
                cbPayItems =
                            [
                                new PayItem
                    {
                        ftPayItemCase = (PayItemCase) 0x4752_2000_0000_0001,
                        Amount = 12.4m,
                        Description = "Cash"
                    }
                            ]
            };
        }
    }

    internal class Mock<T>
    {
        public Mock()
        {
        }
    }
}
