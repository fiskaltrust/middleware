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
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.FR.LNE;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest.SelfCertified;

public class SelfCertifiedFRSSCDTests
{
    private static SelfCertifiedConfiguration Configuration(string privateKeyBase64) => new()
    {
        Siret = FRTestData.Siret,
        PrivateKey = privateKeyBase64,
        CertificateSerialNumber = "CERT-4711",
        SoftwareVersion = "1.0.0",
    };

    [Fact]
    public async Task ProcessReceiptAsync_SignatureVerifiesOverTheSignedDataSet()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new SelfCertifiedFRSSCD(Configuration(privateKey));

        var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        var signature = SignatureOf(response, SignatureTypeFR.ReceiptSignature);
        FRTestData.IsBase64Url(signature).Should().BeTrue();
        key.VerifyData(Encoding.UTF8.GetBytes(DataSetOf(response)), FRTestData.FromBase64Url(signature), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
            .Should().BeTrue("an auditor re-verifies the archived data set with the public key");
    }

    [Fact]
    public async Task ProcessReceiptAsync_EmitsNoCertificationBodyItem()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new SelfCertifiedFRSSCD(Configuration(privateKey));

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        var signatures = response.ReceiptResponse.ftSignatures;
        signatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.CertificationBody), "a self-certified installation has no certification body to mention");
        signatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.ChainHash), "ADR 0001 chains on the signature itself");
        SignatureOf(response, SignatureTypeFR.ReceiptSignature).Should().Be(signature);
        SignatureOf(response, SignatureTypeFR.CertificateSerialNumber).Should().Be("CERT-4711");
        SignatureOf(response, SignatureTypeFR.Siret).Should().Be(FRTestData.Siret);
    }

    [Fact]
    public async Task ProcessReceiptAsync_DataSetEndsWithCounterFlagAndPreviousSignature()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new SelfCertifiedFRSSCD(Configuration(privateKey));

        var (first, firstSignature) = await sut.ProcessReceiptAsync(FRTestData.Request(1), null);
        var (second, _) = await sut.ProcessReceiptAsync(FRTestData.Request(2), firstSignature);

        DataSetOf(first).Split('|')[^3..].Should().Equal("1", "N", "");
        DataSetOf(second).Split('|')[^3..].Should().Equal("2", "O", firstSignature);
    }

    [Fact]
    public async Task ProcessReceiptAsync_BuildsTheSameDataSetAsLne()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var configuration = Configuration(privateKey);
        configuration.Rcs = "RCS Nanterre B 642036065";
        configuration.Naf = "6201Z";
        configuration.LegalForm = "SAS, au capital de 500000 EUR";
        configuration.VatOption = "debits";
        configuration.ElectronicAddress = "642036065";
        using var sut = new SelfCertifiedFRSSCD(configuration);
        using var lne = new LneFRSSCD(new LneConfiguration
        {
            Siret = FRTestData.Siret,
            PrivateKey = privateKey,
            CertificateSerialNumber = "CERT-4711",
            LneCertificateNumber = "LNE-2026-042",
            Rcs = configuration.Rcs,
            Naf = configuration.Naf,
            LegalForm = configuration.LegalForm,
            VatOption = configuration.VatOption,
            ElectronicAddress = configuration.ElectronicAddress,
        });
        // One ReceiptRequest for both: the signed request hash covers its random ftCashBoxID.
        var receiptRequest = FRTestData.Request(3).ReceiptRequest;
        ProcessRequest Request()
        {
            var request = FRTestData.Request(3);
            request.ReceiptRequest = receiptRequest;
            return request;
        }

        var (selfCertified, _) = await sut.ProcessReceiptAsync(Request(), "previous-signature");
        var (certified, _) = await lne.ProcessReceiptAsync(Request(), "previous-signature");

        DataSetOf(selfCertified).Should().Be(DataSetOf(certified), "market-fr#176: same data set and signing as LNE");
        DataSetOf(selfCertified).Split('|')[8].Should().Be(FRCanonicalRequest.SignedField(receiptRequest), "the request hash in its canonical form (ADR 0013)");
    }

    [Fact]
    public async Task ProcessReceiptAsync_WhenTheKeyIsGone_ReturnsTheFailureInsteadOfThrowing()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var sut = new SelfCertifiedFRSSCD(Configuration(privateKey));
        sut.Dispose();

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        FRTestData.ShouldBeASigningFailure(response, signature);
    }

    [Fact]
    public void FromConfiguration_ReadsTheIssuerClaimsLikeLne()
    {
        var configuration = SelfCertifiedConfiguration.FromConfiguration(new Dictionary<string, object>
        {
            ["Siret"] = FRTestData.Siret,
            ["PrivateKey"] = "key",
            ["Rcs"] = "RCS Paris B 1",
            ["VatOption"] = "receipts",
        });

        configuration.Rcs.Should().Be("RCS Paris B 1");
        configuration.VatOption.Should().Be("receipts");
    }

    [Fact]
    public void FromConfiguration_RejectsAnIssuerClaimContainingTheFieldSeparator()
    {
        var act = () => SelfCertifiedConfiguration.FromConfiguration(new Dictionary<string, object>
        {
            ["Siret"] = FRTestData.Siret,
            ["PrivateKey"] = "key",
            ["LegalForm"] = "SAS | 1 EUR",
        });

        act.Should().Throw<FRValidationException>();
    }

    [Fact]
    public async Task ProcessReceiptAsync_ChainContinuesAcrossASwitchFromLne()
    {
        var (lneKey, oldKey) = FRTestData.CreateKey();
        var (selfCertifiedKey, newKey) = FRTestData.CreateKey();
        using var _ = oldKey;
        using var __ = newKey;
        using var lne = new LneFRSSCD(new LneConfiguration { Siret = FRTestData.Siret, PrivateKey = lneKey, CertificateSerialNumber = "CERT-OLD" });
        using var sut = new SelfCertifiedFRSSCD(Configuration(selfCertifiedKey));

        var (_, lneSignature) = await lne.ProcessReceiptAsync(FRTestData.Request(1), null);
        var (second, _) = await sut.ProcessReceiptAsync(FRTestData.Request(2), lneSignature);

        // ADR 0005: the first entry under the new key covers the old key's last signature.
        DataSetOf(second).Split('|')[^3..].Should().Equal("2", "O", lneSignature);
    }

    [Fact]
    public async Task ProcessReceiptAsync_DataSetIsCultureInvariant()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new SelfCertifiedFRSSCD(Configuration(privateKey));

        // One ReceiptRequest for both: the signed request hash covers its random ftCashBoxID.
        var receiptRequest = FRTestData.Request().ReceiptRequest;
        var invariant = await DataSetUnderCulture(sut, CultureInfo.InvariantCulture, receiptRequest);
        var french = await DataSetUnderCulture(sut, new CultureInfo("fr-FR"), receiptRequest);

        french.Should().Be(invariant);
        invariant.Should().Contain("17.50");
    }

    [Fact]
    public async Task PeriodTotals_AreCarriedInTheSignedDataSetOfAClosing()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new SelfCertifiedFRSSCD(Configuration(privateKey));

        var request = FRTestData.Request();
        request.PeriodTotals = new FRPeriodTotals
        {
            Period = FRTotalsPeriod.Day,
            Current = new FRTotals { Totalizer = 25.50m, CINormal = 20.00m },
            Perpetual = new FRTotals { Totalizer = 99.00m },
        };

        var (response, _) = await sut.ProcessReceiptAsync(request, null);

        DataSetOf(response).Should().Contain("Day,25.50,20.00").And.Contain("99.00");
    }

    [Fact]
    public async Task GetInfoAsync_ReportsSelfCertifiedAndTheCertificateSerial()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new SelfCertifiedFRSSCD(Configuration(privateKey));

        var info = await sut.GetInfoAsync();

        info.InfoData.Should().Contain("\"CertificationBody\":\"SelfCertified\"");
        info.InfoData.Should().Contain("\"CertificateSerialNumber\":\"CERT-4711\"");
        info.InfoData.Should().Contain("\"SignatureCreationDataAvailable\":true");
    }

    [Fact]
    public void Constructor_WithUnreadableKey_ThrowsCertificateException()
    {
        var act = () => new SelfCertifiedFRSSCD(Configuration(Convert.ToBase64String(new byte[] { 1, 2, 3 })));

        act.Should().Throw<FRCertificateException>().WithMessage("*secp256r1*");
    }

    [Fact]
    public void Constructor_WithAKeyOnAnotherCurve_IsRejected()
    {
        using var otherCurveKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);

        var act = () => new SelfCertifiedFRSSCD(Configuration(Convert.ToBase64String(otherCurveKey.ExportPkcs8PrivateKey())));

        act.Should().Throw<FRCertificateException>().WithMessage("*P-256*");
    }

    [Fact]
    public void FromConfiguration_NeedsNoCertificationBodyNumber()
    {
        var configuration = SelfCertifiedConfiguration.FromConfiguration(new Dictionary<string, object>
        {
            ["siret"] = FRTestData.Siret,
            ["privateKey"] = "aa",
            ["certificateSerialNumber"] = "CERT-4711",
        });

        configuration.Siret.Should().Be(FRTestData.Siret);
        configuration.CertificateSerialNumber.Should().Be("CERT-4711");
    }

    [Theory]
    [InlineData("PrivateKey", "*Siret*")]
    [InlineData("Siret", "*PrivateKey*")]
    public void FromConfiguration_WithoutAMandatoryValue_IsRejected(string onlyKey, string expectedMessage)
    {
        var act = () => SelfCertifiedConfiguration.FromConfiguration(new Dictionary<string, object> { [onlyKey] = onlyKey == "Siret" ? FRTestData.Siret : "aa" });

        act.Should().Throw<FRValidationException>().WithMessage(expectedMessage);
    }

    private static string DataSetOf(ProcessResponse response) => SignatureOf(response, SignatureTypeFR.Information);

    private static string SignatureOf(ProcessResponse response, SignatureTypeFR type)
        => response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(type)).Data;

    private static async Task<string> DataSetUnderCulture(SelfCertifiedFRSSCD sut, CultureInfo culture, fiskaltrust.ifPOS.v2.ReceiptRequest receiptRequest)
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = culture;
        try
        {
            var request = FRTestData.Request();
            request.ReceiptRequest = receiptRequest;
            var (response, _) = await sut.ProcessReceiptAsync(request, null);
            return DataSetOf(response);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
