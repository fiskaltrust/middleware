﻿using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.me;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using fiskaltrust.Middleware.SCU.ME.Common.Configuration;
using Bogus;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Security.Cryptography;
using System.Collections.Generic;

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
                BusinessUnitCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                InternalTcrIdentifier = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                RequestId = Guid.NewGuid(),
                TcrSoftwareCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                TcrSoftwareMaintainerCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                TcrType = TcrType.Regular
            };

            try
            {
                _ = await meSSCD.RegisterTcrAsync(request);
            }
            catch (Exception ex)
            {
                _ = ex.Message.Should().StartWith("Received certificate doesn't contain TIN number.");
            }
        }

        [Fact]
        public async Task UnregisterTcrRequest()
        {
            IMESSCD meSSCD = CreateSCU();

            var request = new RegisterTcrRequest
            {
                BusinessUnitCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                InternalTcrIdentifier = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                RequestId = Guid.NewGuid(),
            };

            try
            {
                await meSSCD.UnregisterTcrAsync(request);
            }
            catch (Exception ex)
            {
                _ = ex.Message.Should().StartWith("Received certificate doesn't contain TIN number.");
            }
        }

        [Fact]
        public async Task RegisterCashDepositRequest()
        {
            IMESSCD meSSCD = CreateSCU();

            var request = new RegisterCashDepositRequest
            {
                RequestId = Guid.NewGuid(),
                Amount = Math.Round(_faker.Random.Decimal(1000), 2),
                Moment = DateTime.Now,
                TcrCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}"
            };

            try
            {
                _ = await meSSCD.RegisterCashDepositAsync(request);
            }
            catch (Exception ex)
            {
                _ = ex.Message.Should().StartWith("Received certificate doesn't contain TIN number.");
            }
        }

        [Fact]
        public async Task RegisterCashWithdrawalRequest()
        {
            IMESSCD meSSCD = CreateSCU();

            var request = new RegisterCashWithdrawalRequest
            {
                RequestId = Guid.NewGuid(),
                Amount = Math.Round(_faker.Random.Decimal(1000), 2),
                Moment = DateTime.Now,
                TcrCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}"
            };

            try
            {
                _ = await meSSCD.RegisterCashWithdrawalAsync(request);
            }
            catch (Exception ex)
            {
                _ = ex.Message.Should().StartWith("Received certificate doesn't contain TIN number.");
            }
        }

        [Fact(Skip = "Not Implemented Yet")]
        public async Task RegisterInvoiceRequest()
        {
            IMESSCD meSSCD = CreateSCU();

            var request = new RegisterInvoiceRequest
            {
                RequestId = Guid.NewGuid(),
                Moment = DateTime.Now,
                TcrCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                BusinessUnitCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                OperatorCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                SoftwareCode = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                IsIssuerInVATSystem = true,
                InvoiceDetails = new InvoiceDetails
                {
                     GrossAmount = Math.Round(_faker.Random.Decimal(1000), 2),
                     InvoiceType = InvoiceType.Cash,
                     Fees = new List<InvoiceFee>
                     {

                     },
                     ItemDetails = new List<InvoiceItem>
                     {

                     },
                     NetAmount = Math.Round(_faker.Random.Decimal(1000), 2),
                     PaymentDetails = new List<InvoicePayment>
                     {

                     },
                     YearlyOrdinalNumber = _faker.Random.ULong()
                }
            };

            try
            {
                _ = await meSSCD.RegisterInvoiceAsync(request);
            }
            catch (Exception ex)
            {
                _ = ex.Message.Should().StartWith("Received certificate doesn't contain TIN number.");
            }
        }

        private FiscalizationServiceSCU CreateSCU()
        {
            return new FiscalizationServiceSCU(Mock.Of<ILogger<FiscalizationServiceSCU>>(), new ScuMEConfiguration
            {
                Certificate = BuildSelfSignedServerCertificate(),
                PosOperatorAddress = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                PosOperatorCountry = $"{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}{_faker.Random.String(2, 'a', 'z')}{_faker.Random.String(3, '0', '9')}",
                PosOperatorName = _faker.Random.String2(10),
                PosOperatorTown = _faker.Random.String2(10),
                TIN = _faker.Random.String(8, '0', '9'),
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
