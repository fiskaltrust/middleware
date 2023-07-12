using System.IO;
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
            var cert = new X509Certificate2(@"TestCertificates/autonomo_act.p12", "Iz3np32023", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
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


        [Theory]
        [InlineData("TestData/Example_bizkaia_TicketBAI_79732487C_A2022_0399.xml", "TestData/Example_bizkaia_TicketBAI_79732487C_A2022_0399_signed.xml")]
        [InlineData("TestData/Example_bizkaia_TicketBAI_79732487C_A2022_0400.xml", "TestData/Example_bizkaia_TicketBAI_79732487C_A2022_0400_signed.xml")]
        [InlineData("TestData/Example_bizkaia_TicketBAI_B00000034_B2022_0100.xml", "TestData/Example_bizkaia_TicketBAI_B00000034_B2022_0100_signed.xml")]
        [InlineData("TestData/Example_bizkaia_TicketBAI_B00000034_B2022_0101.xml", "TestData/Example_bizkaia_TicketBAI_B00000034_B2022_0101_signed.xml")]
        public void SignXMLTest_ShouldCreateXadesBasedSignature(string xmlFile, string signedXmlFile)
        {
            var cert = new X509Certificate2(@"TestCertificates/autonomo_act.p12", "Iz3np32023", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            var config = new TicketBaiSCUConfiguration
            {
                Certificate = cert
            };
            var sut = new TicketBaiRequestFactory(config);
            var content = File.ReadAllText(xmlFile);
            var signedContent = File.ReadAllText(signedXmlFile);
            var xml = sut.SignXmlContentWithXades(content);
            xml.Should().Be(signedContent);
        }
    }
}
