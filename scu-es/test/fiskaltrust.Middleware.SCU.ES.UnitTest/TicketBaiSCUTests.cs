using System;
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
            var sut = new TicketBaiSCU(NullLogger<TicketBaiSCU>.Instance, config);
            var response = await sut.SubmitInvoiceAsync(new SubmitInvoiceRequest
            {
                InvoiceMoment = DateTime.UtcNow,
                Series = $"T-{DateTime.UtcNow.Ticks}",
                InvoiceNumber = "001"
            });
            _output.WriteLine(FormatXml(response.ResponseContent));
            response.Succeeded.Should().BeTrue(because: response.ResponseContent);
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
            var sut = new TicketBaiSCU(NullLogger<TicketBaiSCU>.Instance, config);
            var response = await sut.SubmitInvoiceAsync(new SubmitInvoiceRequest
            {
                InvoiceMoment = DateTime.UtcNow,
                Series = $"T-{DateTime.UtcNow.Ticks}",
                InvoiceNumber = "001"
            });
            _output.WriteLine(FormatXml(response.ResponseContent));
            response.Succeeded.Should().BeTrue(because: response.ResponseContent);
        }

        [Fact]
        public async Task SubmitBizkaiaInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/Bizkaia/EntitateOrdezkaria_RepresentanteDeEntidad.p12", "IZDesa2021", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert,
                TicketBaiTerritory = TicketBaiTerritory.Araba,
                EmisorNif = "B10646545",
                EmisorApellidosNombreRazonSocial = "CRISTIAN TECH AND CONSULTING S.L."
            };
            var sut = new TicketBaiSCU(NullLogger<TicketBaiSCU>.Instance, config);
            var response = await sut.SubmitInvoiceAsync(new SubmitInvoiceRequest
            {
                InvoiceMoment = DateTime.UtcNow,
                Series = $"T-{DateTime.UtcNow.Ticks}",
                InvoiceNumber = "001"
            });
            _output.WriteLine(FormatXml(response.ResponseContent));
            response.Succeeded.Should().BeTrue(because: response.ResponseContent);
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
