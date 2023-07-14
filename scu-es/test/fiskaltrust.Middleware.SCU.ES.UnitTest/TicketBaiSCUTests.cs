using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using fiskaltrust.Middleware.SCU.ES.TicketBAI;
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

        [Fact]
        public async Task SubmitGipuzkoaInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/Gipuzkoa/dispositivo_act.p12", "Iz3np32023", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert,
                TicketBaiTerritory = TicketBaiTerritory.Gipuzkoa,
                EmisorNif = "B10646545",
                EmisorApellidosNombreRazonSocial = "CRISTIAN TECH AND CONSULTING S.L."
            };
            await PerformTicketBaiRequestChain(config);

        }

        private async Task PerformTicketBaiRequestChain(TicketBaiSCUConfiguration config)
        {
            var sut = new TicketBaiSCU(NullLogger<TicketBaiSCU>.Instance, config);
            var series = $"T-{DateTime.UtcNow.Ticks}";
            var request = new SubmitInvoiceRequest
            {
                InvoiceMoment = DateTime.UtcNow,
                Series = series,
                InvoiceNumber = "001",
                InvoiceLine = new List<InvoiceLine>
                {
                    new InvoiceLine
                    {
                        VATRate = 21.0m,
                        Amount = 121,
                        VATAmount = 21,
                        Description = "test object",
                        Quantity = 1
                    }
                }
            };
            var response = await sut.SubmitInvoiceAsync(request);
            _output.WriteLine(FormatXml(response.ResponseContent));
            response.Succeeded.Should().BeTrue(because: response.ResponseContent);

            var response2 = await sut.SubmitInvoiceAsync(new SubmitInvoiceRequest
            {
                InvoiceMoment = DateTime.UtcNow,
                Series = series,
                InvoiceNumber = "002",
                LastInvoiceMoment = request.LastInvoiceMoment,
                LastInvoiceNumber = request.InvoiceNumber,
                LastInvoiceSignature = response.ShortSignatureValue,
                InvoiceLine = new List<InvoiceLine>
                {
                    new InvoiceLine
                    {
                        VATRate = 21.0m,
                        Amount = 121,
                        VATAmount = 21,
                        Description = "test object",
                        Quantity = 1
                    }
                }
            });

            _output.WriteLine(FormatXml(response2.ResponseContent));
            response2.Succeeded.Should().BeTrue(because: response2.ResponseContent);
        }

        [Fact]
        public async Task SubmitArabaInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/Araba/dispositivo_act.p12", "Iz3np32023", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert,
                TicketBaiTerritory = TicketBaiTerritory.Araba,
                EmisorNif = "B10646545",
                EmisorApellidosNombreRazonSocial = "CRISTIAN TECH AND CONSULTING S.L."
            };
            await PerformTicketBaiRequestChain(config);
        }

        [Fact]
        public async Task SubmitBizkaiaInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/Bizkaia/EntitateOrdezkaria_RepresentanteDeEntidad.p12", "IZDesa2021", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert,
                TicketBaiTerritory = TicketBaiTerritory.Bizkaia,
                EmisorNif = "B10646545",
                EmisorApellidosNombreRazonSocial = "CRISTIAN TECH AND CONSULTING S.L."
            };
            await PerformTicketBaiRequestChain(config);
        }

        private string FormatXml(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                return doc.ToString();
            }
            catch (Exception)
            {
                return xml;
            }
        }
    }
}
