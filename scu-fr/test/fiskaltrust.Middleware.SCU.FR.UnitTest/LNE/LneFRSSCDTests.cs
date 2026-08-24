using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.LNE;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest.LNE;

public class LneFRSSCDTests
{
    private static LneConfiguration Configuration(string privateKeyBase64) => new()
    {
        Siret = FRTestData.Siret,
        PrivateKey = privateKeyBase64,
        CertificateSerialNumber = "CERT-4711",
        LneCertificateNumber = "LNE-2026-042",
        SoftwareVersion = "1.0.0",
    };

    [Fact]
    public async Task ProcessReceiptAsync_SignatureVerifiesOverTheSignedDataSet()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var (response, _) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);

        var signatures = response.ReceiptResponse.ftSignatures;
        var dataSet = signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Information)).Data;
        var signature = signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;

        key.VerifyData(Encoding.UTF8.GetBytes(dataSet), Convert.FromBase64String(signature), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            .Should().BeTrue("an LNE audit re-verifies the archived data set field by field");
    }

    [Fact]
    public async Task ProcessReceiptAsync_ChainHashCoversTheSignature()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var (response, hash) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);

        var signatures = response.ReceiptResponse.ftSignatures;
        var dataSet = signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Information)).Data;
        var signature = signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;

        var expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes($"{dataSet}|{signature}")));
        hash.Should().Be(expected);
        signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ChainHash)).Data.Should().Be(hash);
    }

    [Fact]
    public async Task ProcessReceiptAsync_DataSetCarriesThePreviousHashAsItsLastField()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var (_, firstHash) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);
        var (second, secondHash) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, firstHash);

        var dataSet = second.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Information)).Data;
        dataSet.Split('|').Last().Should().Be(firstHash);
        secondHash.Should().NotBe(firstHash);
    }

    [Fact]
    public async Task ProcessReceiptAsync_DataSetIsCultureInvariant()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var invariant = await DataSetUnderCulture(sut, CultureInfo.InvariantCulture);
        // fr-FR writes decimals with a comma and would silently change every signed amount.
        var french = await DataSetUnderCulture(sut, new CultureInfo("fr-FR"));

        french.Should().Be(invariant);
        invariant.Should().Contain("17.50");
    }

    [Fact]
    public async Task GetInfoAsync_ReportsTheCertificationBodyInTheInfoDataBlob()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var info = await sut.GetInfoAsync();

        info.InfoData.Should().Contain("\"CertificationBody\":\"LNE\"");
        info.InfoData.Should().Contain("LNE-2026-042");
    }

    [Fact]
    public void Constructor_WithUnreadableKey_ThrowsCertificateException()
    {
        var act = () => new LneFRSSCD(Configuration(Convert.ToBase64String(new byte[] { 1, 2, 3 })));

        act.Should().Throw<FRCertificateException>().WithMessage("*secp256r1*");
    }

    [Fact]
    public void FromConfiguration_WithoutSiret_IsRejected()
    {
        var act = () => LneConfiguration.FromConfiguration(new Dictionary<string, object> { ["PrivateKey"] = "aa" });

        act.Should().Throw<FRValidationException>().WithMessage("*Siret*");
    }

    private static async Task<string> DataSetUnderCulture(LneFRSSCD sut, CultureInfo culture)
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = culture;
        try
        {
            var (response, _) = await sut.ProcessReceiptAsync(new ProcessRequest { ReceiptRequest = FRTestData.CashSaleRequest(), ReceiptResponse = FRTestData.Response() }, null);
            return response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Information)).Data;
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
