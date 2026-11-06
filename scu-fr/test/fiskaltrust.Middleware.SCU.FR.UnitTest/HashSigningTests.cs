using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.Middleware.SCU.FR.InMemory;
using fiskaltrust.Middleware.SCU.FR.LNE;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>
/// Archive signing by the SCU (ADR 0007 amendment, market-fr#218): the queue signs a locally
/// exported archive's hashes with the SCU key, and embeds the SCU certificate. A failure comes back
/// in the result, never as an exception.
/// </summary>
public class HashSigningTests
{
    private static readonly byte[] Hash = SHA256.HashData(Encoding.UTF8.GetBytes("manifest"));

    private static string Certificate(ECDsa key)
    {
        using var certificate = new CertificateRequest("CN=SCU FR", key, HashAlgorithmName.SHA256).CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return Convert.ToBase64String(certificate.RawData);
    }

    private static IFRSSCD Lne(string privateKey, string? certificate) => new LneFRSSCD(new LneConfiguration
    {
        Siret = FRTestData.Siret,
        PrivateKey = privateKey,
        CertificateBase64 = certificate,
        CertificateSerialNumber = "CERT-4711",
    });

    private static IFRSSCD InfoCert(string privateKey, string? certificate) => new InfoCertFRSSCD(new InfoCertConfiguration
    {
        Siret = FRTestData.Siret,
        PrivateKey = privateKey,
        CertificateBase64 = certificate,
        CertificateSerialNumber = "CERT-4711",
    });

    private static IFRSSCD SelfCertified(string privateKey, string? certificate) => new SelfCertifiedFRSSCD(new SelfCertifiedConfiguration
    {
        Siret = FRTestData.Siret,
        PrivateKey = privateKey,
        CertificateBase64 = certificate,
        CertificateSerialNumber = "CERT-4711",
    });

    public static TheoryData<string> CertifiedScus => new() { "LNE", "InfoCert", "SelfCertified" };

    private static IFRSSCD Scu(string name, string privateKey, string? certificate)
        => name switch
        {
            "LNE" => Lne(privateKey, certificate),
            "SelfCertified" => SelfCertified(privateKey, certificate),
            _ => InfoCert(privateKey, certificate),
        };

    private static bool Verifies(FRSigningCertificate signingCertificate, FRHashSignature signature)
    {
        using var certificate = new X509Certificate2(Convert.FromBase64String(signingCertificate.CertificateBase64));
        using var publicKey = certificate.GetECDsaPublicKey()!;
        return publicKey.VerifyHash(Hash, signature.Signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    [Theory]
    [MemberData(nameof(CertifiedScus))]
    public async Task SignHashAsync_VerifiesAgainstTheConfiguredCertificate(string name)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var sut = Scu(name, privateKey, Certificate(key));

        var signingCertificate = await sut.GetSigningCertificateAsync();
        var signature = await sut.SignHashAsync(Hash);

        signingCertificate.Failure.Should().BeNull();
        signature.Failure.Should().BeNull();
        signingCertificate.SignatureAlgorithm.Should().Be(FRSigningCertificate.ES256);
        signingCertificate.CertificateSerialNumber.Should().Be("CERT-4711");
        Verifies(signingCertificate, signature).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(CertifiedScus))]
    public async Task GetSigningCertificateAsync_ReportsAMissingCertificate(string name)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;

        var result = await Scu(name, privateKey, null).GetSigningCertificateAsync();

        result.Failure.Should().Match("*CertificateBase64*");
        result.CertificateBase64.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(CertifiedScus))]
    public async Task GetSigningCertificateAsync_ReportsACertificateOfAnotherKey(string name)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var _ = key;

        var result = await Scu(name, privateKey, Certificate(other)).GetSigningCertificateAsync();

        result.Failure.Should().Match("*public key*");
    }

    [Theory]
    [MemberData(nameof(CertifiedScus))]
    public async Task SignHashAsync_ReportsAnythingButASha256(string name)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;

        var result = await Scu(name, privateKey, Certificate(key)).SignHashAsync(new byte[20]);

        result.Failure.Should().Match("*32 bytes*");
        result.Signature.Should().BeEmpty();
    }

    [Fact]
    public async Task InMemorySCU_SignsWithASelfSignedCertificate()
    {
        using var sut = new InMemorySCU();

        var signingCertificate = await sut.GetSigningCertificateAsync();
        var signature = await sut.SignHashAsync(Hash);

        Verifies(signingCertificate, signature).Should().BeTrue();
    }
}
