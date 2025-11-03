using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
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
            var response = await sut.SendAsync(request, territory.SubmitInvoices);
            _output.WriteLine(FormatXml(response.ResponseContent));
            response.Succeeded.Should().BeTrue(because: response.ResponseContent);

            var response2 = await sut.SendAsync(new SubmitInvoiceRequest
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
            }, territory.SubmitInvoices);

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
