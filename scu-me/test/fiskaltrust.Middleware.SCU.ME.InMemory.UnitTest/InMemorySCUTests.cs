using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Bogus;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.SCU.ME.Common.Configuration;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ME.InMemory.UnitTest
{
    public class InMemorySCUTests
    {
        private readonly Faker _faker = new Faker();

        [Fact]
        public async Task RegisterTcr()
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

            _ = response.TcrCode.Should().MatchRegex("[a-z]{2}[0-9]{3}[a-z]{2}[0-9]{3}");
        }

        [Fact]
        public async Task UnregisterTcr()
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

            await meSSCD.UnregisterTcrAsync(request);
        }

        [Fact]
        public async Task RegisterCashDeposit()
        {
            IMESSCD meSSCD = CreateSCU();

            var request = new RegisterCashDepositRequest
            {
                RequestId = Guid.NewGuid(),
                Amount = _faker.Random.Decimal(0, 1000),
                Moment = DateTime.Now,
                TcrCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}"
            };

            var response = await meSSCD.RegisterCashDepositAsync(request);

            _ = Guid.Parse(response.FCDC);
        }

        [Fact]
        public async Task RegisterCashWithdrawal()
        {

            IMESSCD meSSCD = CreateSCU();

            var request = new RegisterCashWithdrawalRequest
            {
                RequestId = Guid.NewGuid(),
                Amount = _faker.Random.Decimal(0, 1000),
                Moment = DateTime.Now,
                TcrCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}"
            };

            _ = await meSSCD.RegisterCashWithdrawalAsync(request);
        }

        [Fact]
        public async Task RegisterInvoice()
        {

            IMESSCD meSSCD = CreateSCU();

            var request = new RegisterInvoiceRequest
            {
                BusinessUnitCode = _faker.Random.String2(10),
                RequestId = Guid.NewGuid(),
                InvoiceDetails = new InvoiceDetails
                {
                    YearlyOrdinalNumber = _faker.Random.ULong(),
                    GrossAmount = _faker.Random.Decimal(0, 1000)
                },
                IsIssuerInVATSystem = true,
                Moment = DateTime.Now,
                OperatorCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                SoftwareCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                TcrCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}"
            };

            var response = await meSSCD.RegisterInvoiceAsync(request);

            _ = Guid.Parse(response.FIC);
            _ = response.IIC.Should().HaveLength(32);
        }

        private InMemorySCU CreateSCU()
        {
            return new InMemorySCU(new ScuMEConfiguration
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

                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, "password"), "password", X509KeyStorageFlags.MachineKeySet);
            }
        }
    }
}