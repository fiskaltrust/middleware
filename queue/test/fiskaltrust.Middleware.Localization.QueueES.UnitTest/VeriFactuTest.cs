using System.Security.Cryptography.X509Certificates;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;
using fiskaltrust.Middleware.Localization.QueueES.Exports;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.ES.Models;
using fiskaltrust.Middleware.SCU.ES.Soap;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueES.UnitTest
{
    public class VeriFactuTest()
    {
        [Fact, Trait("only", "local")]
        public async Task ResetReceipts()
        {
            var certificate = new X509Certificate2(await File.ReadAllBytesAsync("Certificates/Certificado_RPJ_A39200019_CERTIFICADO_ENTIDAD_PRUEBAS_4_Pre.p12"), "1234");

            var masterData = new MasterDataConfiguration()
            {
                Account = new AccountMasterData
                {
                    VatId = "M0291081Q",
                    AccountName = "Thomas Steininger"
                }
            };
            var queueItemRepository = new Mock<IMiddlewareQueueItemRepository>();

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

            var veriFactuMapping = new VeriFactuMapping(masterData, certificate);
            var journalES = veriFactuMapping.CreateRegistroFacturacionAlta(receiptRequest, receiptResponse, null, null);

            var client = new Client(new Uri(new VeriFactuSCUConfiguration().BaseUrl), certificate);

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
            response.IsOk.Should().BeTrue($"Response should not be error\n{response.ErrValue?.ToString()}\n");
            response.OkValue!.RespuestaLinea.Should().HaveCount(1);
            response.OkValue!.RespuestaLinea!.First().CodigoErrorRegistro.Should().BeNullOrEmpty();
            response.OkValue!.RespuestaLinea!.First().DescripcionErrorRegistro.Should().BeNullOrEmpty();
        }

        [Fact, Trait("only", "local")]
        public async Task VeriFactuTestInit()
        {
            var certificate = new X509Certificate2(await File.ReadAllBytesAsync("Certificates/Certificado_RPJ_A39200019_CERTIFICADO_ENTIDAD_PRUEBAS_4_Pre.p12"), "1234");

            var masterData = new MasterDataConfiguration()
            {
                Account = new AccountMasterData
                {
                    VatId = "M0291081Q",
                    AccountName = "Thomas Steininger"
                }
            };
            var queueItemRepository = new Mock<IMiddlewareQueueItemRepository>();

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

            var veriFactuMapping = new VeriFactuMapping(masterData, certificate);
            var journalES = veriFactuMapping.CreateRegistroFacturacionAlta(receiptRequest, receiptResponse, null, null);

            var client = new Client(new Uri(new VeriFactuSCUConfiguration().BaseUrl), certificate);

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
}
