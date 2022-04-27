using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.me;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using fiskaltrust.Middleware.SCU.ME.Common.Configuration;
using fiskaltrust.Middleware.SCU.ME.FiscalizationService;
using Bogus;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Security.Cryptography;

namespace fiskaltrust.Middleware.SCU.ME.FiscalizationService.UnitTest
{
    public class InMemorySCUTests
    {
        private readonly Faker _faker = new Faker();

        [Fact]
        public async Task RegisterTcrRequest()
        {

            IMESSCD meSSCD = CreateSCU();

            var request = new RegisterTcrRequest
            {
                BusinessUnitCode = _faker.Random.String2(10),
                InternalTcrIdentifier = _faker.Random.String2(10),
                RequestId = Guid.NewGuid(),
                TcrSoftwareCode = _faker.Random.String2(10),
                TcrSoftwareMaintainerCode = _faker.Random.String2(10),
                TcrType = TcrType.Regular
            };

            var response = await meSSCD.RegisterTcrAsync(request);

            response.TcrCode.Should().NotBeNullOrEmpty();

        }
        private FiscalizationServiceSCU CreateSCU()
        {
            return new FiscalizationServiceSCU(Mock.Of<ILogger<FiscalizationServiceSCU>>(), new ScuMEConfiguration
            {
                Certificate = BuildSelfSignedServerCertificate(),
                PosOperatorAddress = _faker.Random.String2(10),
                PosOperatorCountry = _faker.Random.String2(10),
                PosOperatorName = _faker.Random.String2(10),
                PosOperatorTown = _faker.Random.String2(10),
                TIN = _faker.Random.String2(10),
                VatNumber = _faker.Random.String(10, '0', '9')
            });
        }

        private X509Certificate2 BuildSelfSignedServerCertificate()
        {
            var certificateName = "UnitTests";
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddDnsName(Environment.MachineName);

            var distinguishedName = new X500DistinguishedName($"CN={certificateName}");

            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                {
                    CertificateExtensions = {
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false),
                    new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false),
                    sanBuilder.Build()
                    }
                };

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
                certificate.FriendlyName = certificateName;

                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "WeNeedASaf3rPassword"), "WeNeedASaf3rPassword", X509KeyStorageFlags.MachineKeySet);
            }
        }
    }
}
