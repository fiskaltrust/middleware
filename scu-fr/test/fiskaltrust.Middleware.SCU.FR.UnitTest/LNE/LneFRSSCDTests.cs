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

        var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        var signatures = response.ReceiptResponse.ftSignatures;
        var dataSet = signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Information)).Data;
        var signature = signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;

        FRTestData.IsBase64Url(signature).Should().BeTrue();
        key.VerifyData(Encoding.UTF8.GetBytes(dataSet), FRTestData.FromBase64Url(signature), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            .Should().BeTrue("an LNE audit re-verifies the archived data set field by field");
    }

    [Fact]
    public async Task ProcessReceiptAsync_ReturnsTheStoredSignature_AndNoChainHash()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        var signatures = response.ReceiptResponse.ftSignatures;
        signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data.Should().Be(signature);
        signatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.ChainHash), "ADR 0001 chains on the signature itself");
    }

    [Fact]
    public async Task ProcessReceiptAsync_DataSetEndsWithCounterFlagAndPreviousSignature()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var (first, firstSignature) = await sut.ProcessReceiptAsync(FRTestData.Request(1), null);
        var (second, _) = await sut.ProcessReceiptAsync(FRTestData.Request(2), firstSignature);

        DataSetOf(first).Split('|')[^3..].Should().Equal("1", "N", "");
        DataSetOf(second).Split('|')[^3..].Should().Equal("2", "O", firstSignature);
    }

    [Fact]
    public async Task ProcessReceiptAsync_ChainContinuesAcrossAnScuSwitch()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var infocert = new fiskaltrust.Middleware.SCU.FR.InfoCert.InfoCertFRSSCD(new fiskaltrust.Middleware.SCU.FR.InfoCert.InfoCertConfiguration
        {
            Siret = FRTestData.Siret,
            PrivateKey = privateKey,
            CertificateSerialNumber = "CERT-4711",
        });
        using var lne = new LneFRSSCD(Configuration(privateKey));

        var (_, infocertSignature) = await infocert.ProcessReceiptAsync(FRTestData.Request(1), null);
        var (second, _) = await lne.ProcessReceiptAsync(FRTestData.Request(2), infocertSignature);

        // ADR 0005: the rule is shared, so the LNE entry chains on the Infocert entry's signature.
        DataSetOf(second).Split('|')[^3..].Should().Equal("2", "O", infocertSignature);
    }

    private static string DataSetOf(ProcessResponse response)
        => response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Information)).Data;

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

    [Theory]
    [InlineData("nistP384")]
    [InlineData("nistP521")]
    public void Constructor_WithAKeyOnAnotherCurve_IsRejected(string curveName)
    {
        using var otherCurveKey = ECDsa.Create(ECCurve.CreateFromFriendlyName(curveName));
        var privateKey = Convert.ToBase64String(otherCurveKey.ExportPkcs8PrivateKey());

        var act = () => new LneFRSSCD(Configuration(privateKey));

        act.Should().Throw<FRCertificateException>().WithMessage("*P-256*");
    }

    [Fact]
    public async Task PeriodTotals_AreCarriedInTheSignedDataSetOfAClosing()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var request = new ProcessRequest
        {
            ReceiptRequest = FRTestData.CashSaleRequest(),
            ReceiptResponse = FRTestData.Response(),
            PeriodTotals = new FRPeriodTotals
            {
                Period = FRTotalsPeriod.Day,
                Current = new FRTotals { Totalizer = 25.50m, CINormal = 20.00m },
                Perpetual = new FRTotals { Totalizer = 99.00m },
            },
        };

        var (response, _) = await sut.ProcessReceiptAsync(request, null);

        var dataSet = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Information)).Data;
        dataSet.Should().Contain("Day,25.50,20.00");
        dataSet.Should().Contain("99.00", "the perpetual total is reported on every closing");
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
            var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);
            return response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Information)).Data;
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
