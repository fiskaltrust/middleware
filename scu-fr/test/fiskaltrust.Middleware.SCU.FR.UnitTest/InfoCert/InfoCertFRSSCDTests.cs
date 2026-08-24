using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest.InfoCert;

public class InfoCertFRSSCDTests
{
    private static InfoCertConfiguration Configuration(string privateKeyBase64) => new()
    {
        Siret = FRTestData.Siret,
        PrivateKey = privateKeyBase64,
        CertificateSerialNumber = "CERT-4711",
        AttestationNumber = "IC-2026-001",
        SoftwareVersion = "1.0.0",
    };

    [Fact]
    public async Task ProcessReceiptAsync_ProducesCompactJwsWithEs256Header()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var (response, hash) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);

        var jws = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;
        var parts = jws.Split('.');
        parts.Should().HaveCount(3);
        Encoding.UTF8.GetString(FromBase64Url(parts[0])).Should().Be("{\"alg\":\"ES256\",\"typ\":\"JWT\"}");
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessReceiptAsync_SignatureVerifiesAgainstTheConfiguredKey()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var (response, _) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);

        var jws = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;
        var parts = jws.Split('.');
        var signingInput = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");

        key.VerifyData(signingInput, FromBase64Url(parts[2]), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            .Should().BeTrue("the JWS signature must verify against the configured signature creation data");
    }

    [Fact]
    public async Task ProcessReceiptAsync_PayloadCarriesThePreviousHash_SoTheChainIsLinked()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var (first, firstHash) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);
        var (second, secondHash) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, firstHash);

        secondHash.Should().NotBe(firstHash, "an entry covering a different previous hash must hash differently");

        using var payload = JsonDocument.Parse(FromBase64Url(SignatureOf(second).Split('.')[1]));
        payload.RootElement.GetProperty("lastHash").GetString().Should().Be(firstHash, "the signed payload has to carry the previous entry's hash");
        SignatureOf(first).Should().NotBe(SignatureOf(second));
    }

    [Fact]
    public async Task ProcessReceiptAsync_AttachesTheAuditSignatures()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var (response, hash) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);

        var signatures = response.ReceiptResponse.ftSignatures;
        signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ChainHash)).Data.Should().Be(hash);
        signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.CertificateSerialNumber)).Data.Should().Be("CERT-4711");
        signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Siret)).Data.Should().Be(FRTestData.Siret);
        signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.CertificationBody)).Data.Should().Be("Infocert IC-2026-001");
        signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).ftSignatureFormat.Should().Be(SignatureFormat.QRCode);
    }

    [Fact]
    public async Task GetInfoAsync_ReportsTheCertificationBodyInTheInfoDataBlob()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var info = await sut.GetInfoAsync();

        info.InfoData.Should().Contain("\"CertificationBody\":\"Infocert\"");
        info.Description.Should().Contain(FRTestData.Siret);
    }

    [Fact]
    public void Constructor_WithUnreadableKey_ThrowsCertificateException()
    {
        var act = () => new InfoCertFRSSCD(Configuration(Convert.ToBase64String(new byte[] { 1, 2, 3 })));

        act.Should().Throw<FRCertificateException>().WithMessage("*secp256r1*");
    }

    [Fact]
    public void FromConfiguration_WithoutSiret_IsRejected()
    {
        var act = () => InfoCertConfiguration.FromConfiguration(new Dictionary<string, object> { ["PrivateKey"] = "aa" });

        act.Should().Throw<FRValidationException>().WithMessage("*Siret*");
    }

    [Fact]
    public void FromConfiguration_WithoutPrivateKey_IsRejected()
    {
        var act = () => InfoCertConfiguration.FromConfiguration(new Dictionary<string, object> { ["Siret"] = FRTestData.Siret });

        act.Should().Throw<FRValidationException>().WithMessage("*PrivateKey*");
    }

    private static string SignatureOf(ProcessResponse response)
        => response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '='));
    }
}
