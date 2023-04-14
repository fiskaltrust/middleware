using System.Security.Cryptography.X509Certificates;
using fiskaltrust.Middleware.SCU.ES.TicketBAI;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.UnitTest
{
    public class TicketBaiRequestFactoryTests
    {
        [Fact]
        public void CreateSignedXmlContent_Should_Create_SignedXml()
        {
            var cert = new X509Certificate2(@"TestCertificates/PertsonaFisikoa_PersonaFísica.p12", "IZDesa2021", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert
            };
            var sut = new TicketBaiRequestFactory(config);

            var request = new TicketBaiRequest
            {
                Cabecera = new(),
                Factura = new(),
                HuellaTBAI = new(),
                Sujetos = new()
            };
            var xml = sut.CreateSignedXmlContent(request);
            xml.Should().NotBeNull();
        }
    }
}
