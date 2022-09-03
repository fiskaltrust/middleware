using System.Security.Cryptography.X509Certificates;
using System.Text;
using fiskaltrust.Middleware.SCU.AT.Pfx;
using FluentAssertions;

namespace fiskaltrust.Middleware.SCU.AT.InMemory.UnitTest
{
    public class InMemorySCUTests
    {
        [Fact]
        public void Echo_ShouldReturn_InputText()
        {
            var text = "Hello, SCU!";
            var sut = new InMemorySCU();
            sut.Echo(text).Should().Be(text);
        }

        [Fact]
        public void ZDA_ShouldReturn_PFX()
        {
            var sut = new InMemorySCU();
            sut.ZDA().Should().Be("PFX");
        }

        [Fact]
        public void Cert_ShouldReturn_ValidCertificate()
        {
            var sut = new InMemorySCU();
            var cert = sut.Certificate();
            cert.Should().NotBeNullOrEmpty();
            
            var x509 = new X509Certificate(cert);
            x509.Should().NotBeNull();
            x509.Issuer.Should().Be("CN=fiskaltrust DEMO");
        }

        [Fact]
        public void Sign_ShouldReturn_SignedData()
        {
            var testData = Encoding.UTF8.GetBytes("Testdata");
            var sut = new InMemorySCU();

            sut.Sign(testData).Should().NotBeNullOrEmpty();
        }
    }
}