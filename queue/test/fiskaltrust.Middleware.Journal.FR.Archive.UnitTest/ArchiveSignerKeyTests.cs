using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using fiskaltrust.Middleware.Journal.FR.Archive.Signing;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.FR.Archive.UnitTest
{
    /// <summary>
    /// An RSA archive signer holds a key of at least 2048 bits (Ref LNE 1.8 names 1024 as the floor and recommends 2048;
    /// ANSSI RGS asks 2048): a weaker key is refused before anything is signed ([08] RSA path).
    /// </summary>
    public class ArchiveSignerKeyTests
    {
        private static X509Certificate2 Rsa(int bits)
        {
            using var rsa = RSA.Create(bits);
            var request = new CertificateRequest("CN=fiskaltrust archive test RSA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }

        [Fact]
        public void AnRsaKeyUnder2048Bits_IsRefused()
        {
            using var certificate = Rsa(1024);

            ((Action) (() => new X509ArchiveSigner(certificate))).Should().Throw<ArgumentException>().WithMessage("*2048*");
        }

        [Fact]
        public void A2048BitRsaKey_IsTaken()
        {
            using var certificate = Rsa(2048);

            new X509ArchiveSigner(certificate).SignatureAlgorithm.Should().Be(ArchiveSignatureAlgorithms.RS256);
        }
    }
}
