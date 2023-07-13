using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.ES.TicketBAI;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.UnitTest
{
    public class TicketBaiSCUTests
    {
        [Fact]
        public async Task SubmitGipuzkoaInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/Gipuzkoa/dispositivo_act.p12", "Iz3np32023", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert,
                TicketBaiTerritory = TicketBaiTerritory.Gipuzkoa
            };            
            var sut = new TicketBaiSCU(NullLogger<TicketBaiSCU>.Instance, config);
            var req = TicketBaiDemo.GetTicketBayRequest();
            req.Factura.CabeceraFactura.SerieFactura = $"T-{DateTime.Today.Ticks}";
            req.Factura.CabeceraFactura.NumFactura = "1";
            var response = await sut.SubmitInvoiceAsync(req);
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task SubmitArabaInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/Araba/dispositivo_act.p12", "Iz3np32023", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert,
                TicketBaiTerritory = TicketBaiTerritory.Araba
            };
            var sut = new TicketBaiSCU(NullLogger<TicketBaiSCU>.Instance, config);
            var req = TicketBaiDemo.GetTicketBayRequest();
            req.Factura.CabeceraFactura.SerieFactura = $"T-{DateTime.Today.Ticks}";
            req.Factura.CabeceraFactura.NumFactura = "001";
            var response = await sut.SubmitInvoiceAsync(req);
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task SubmitBizkaiaInvoiceAsync()
        {
            var cert = new X509Certificate2(@"TestCertificates/Bizkaia/EntitateOrdezkaria_RepresentanteDeEntidad.p12", "IZDesa2021", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert,
                TicketBaiTerritory = TicketBaiTerritory.Bizkaia
            };
            var sut = new TicketBaiSCU(NullLogger<TicketBaiSCU>.Instance, config);
            var req = TicketBaiDemo.GetTicketBayRequest();
            req.Factura.CabeceraFactura.SerieFactura = $"T-{DateTime.Today.Ticks}";
            req.Factura.CabeceraFactura.NumFactura = "001";
            var response = await sut.SubmitInvoiceAsync(req);
            response.Should().NotBeNull();
        }
    }
}
