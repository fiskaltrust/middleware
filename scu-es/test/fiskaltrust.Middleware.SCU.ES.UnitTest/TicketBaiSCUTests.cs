using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Territories;
using fiskaltrust.Middleware.SCU.ES.TicketBAIAraba;
using fiskaltrust.Middleware.SCU.ES.TicketBAIBizkaia;
using fiskaltrust.Middleware.SCU.ES.TicketBAIGipuzkoa;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.SCU.ES.UnitTest
{
    public class TicketBaiSCUTests
    {
        private readonly ITestOutputHelper _output;

        public TicketBaiSCUTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "Gipuzkoa certificate is not working")]
        public async Task SubmitGipuzkoaInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/Gipuzkoa/dispositivo_act.p12", "Iz3np32023", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert,
                EmisorNif = "B10646545",
                EmisorApellidosNombreRazonSocial = "CRISTIAN TECH AND CONSULTING S.L."
            };
            await PerformTicketBaiRequestChain(config, new TicketBaiGipuzkoaTerritory());

        }

        private async Task PerformTicketBaiRequestChain(TicketBaiSCUConfiguration config, ITicketBaiTerritory territory)
        {
            var sut = new TicketBaiSCU(NullLogger<TicketBaiSCU>.Instance, config, territory);

            var request = new ReceiptRequest
            {
                cbReceiptReference = "001",
                cbChargeItems = [
                        new ChargeItem {
                            VATRate = 21.0m,
                            Amount = 121,
                            VATAmount = 21,
                            Description = "test object",
                            Quantity = 1
                        }
                    ]
            };
            var response = await sut.ProcessReceiptAsync(JsonSerializer.Deserialize<ProcessRequest>(JsonSerializer.Serialize(new ProcessRequest
            {
                ReceiptRequest = request,
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptMoment = DateTime.Now,
                    ftCashBoxIdentification = "test",
                    ftStateData = new MiddlewareStateData
                    {
                        ES = new MiddlewareStateDataES { }
                    }
                },
            })));
            var responseContent = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            _output.WriteLine(responseContent);
            response.ReceiptResponse.ftState.State().Should().Be(State.Success, because: responseContent);

            var response2 = await sut.ProcessReceiptAsync(JsonSerializer.Deserialize<ProcessRequest>(JsonSerializer.Serialize(new ProcessRequest
            {
                ReceiptRequest = new ReceiptRequest
                {
                    cbReceiptReference = "002",
                    cbChargeItems = [
                        new ChargeItem {
                            VATRate = 21.0m,
                            Amount = 121,
                            VATAmount = 21,
                            Description = "test object",
                            Quantity = 1
                        }
                    ]
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptMoment = DateTime.Now,
                    ftCashBoxIdentification = "test",
                    ftStateData = new MiddlewareStateData
                    {
                        ES = new MiddlewareStateDataES
                        {
                            LastReceipt = new Receipt
                            {
                                Request = request,
                                Response = response.ReceiptResponse
                            }
                        }
                    }
                }
            })));

            var response2Content = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            _output.WriteLine(response2Content);
            response2.ReceiptResponse.ftState.State().Should().Be(State.Success, because: response2Content);
        }

        [Fact]
        public async Task SubmitArabaInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/Araba/dispositivo_act.p12", "Iz3np32023", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert,
                EmisorNif = "B10646545",
                EmisorApellidosNombreRazonSocial = "CRISTIAN TECH AND CONSULTING S.L."
            };
            await PerformTicketBaiRequestChain(config, new TicketBaiArabaTerritory());
        }

        [Fact(Skip = "Bizkaia certificate is not working")]
        public async Task SubmitBizkaiaInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/Bizkaia/EntitateOrdezkaria_RepresentanteDeEntidad.p12", "IZDesa2021", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert,
                EmisorNif = "B10646545",
                EmisorApellidosNombreRazonSocial = "CRISTIAN TECH AND CONSULTING S.L."
            };
            await PerformTicketBaiRequestChain(config, new TicketBaiBizkaiaTerritory());
        }
    }
}
