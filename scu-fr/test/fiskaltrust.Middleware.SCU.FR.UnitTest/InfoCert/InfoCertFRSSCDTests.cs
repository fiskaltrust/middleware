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
using fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;
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
        AttestationNumber = "0546",
        SoftwareCategory = "B",
        SoftwareName = "OnQPMS",
        SoftwareVersion = "06.07",
    };

    [Fact]
    public async Task ProcessReceiptAsync_ProducesCompactJwsWithEs256Header()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        var jws = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;
        var parts = jws.Split('.');
        parts.Should().HaveCount(3);
        Encoding.UTF8.GetString(FromBase64Url(parts[0])).Should().Be("{\"alg\":\"ES256\",\"typ\":\"JWT\",\"kid\":\"CERT-4711\"}", "the certificate serial is signed as the header's kid");
        signature.Should().Be(parts[2], "the chained value is the JWS signature segment, never a re-encoding of it");
        FRTestData.IsBase64Url(signature).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessReceiptAsync_SignatureVerifiesAgainstTheConfiguredKey()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        var jws = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;
        var parts = jws.Split('.');
        var signingInput = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");

        key.VerifyData(signingInput, FromBase64Url(parts[2]), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            .Should().BeTrue("the JWS signature must verify against the configured signature creation data");
    }

    [Fact]
    public async Task ProcessReceiptAsync_PayloadCarriesCounterFlagAndPreviousSignature_SoTheChainIsLinked()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var secondRequest = FRTestData.Request(2);
        secondRequest.ReceiptResponse.ftReceiptIdentification = "T2";
        var (first, firstSignature) = await sut.ProcessReceiptAsync(FRTestData.Request(1), null);
        var (second, _) = await sut.ProcessReceiptAsync(secondRequest, firstSignature);

        firstSignature.Should().Be(SignatureOf(first).Split('.')[2]);

        // R13 note/ticket string: the number (TAG-TIK-NUM = ftReceiptIdentification) carries the counter, then flag and previous signature.
        var firstFields = PayloadOf(first).Split(',');
        firstFields[3].Should().Be("T1");
        firstFields[^2].Should().Be("N", "the first entry starts the chain");
        firstFields[^1].Should().BeEmpty();

        var secondFields = PayloadOf(second).Split(',');
        secondFields[3].Should().Be("T2");
        secondFields[^2].Should().Be("O");
        secondFields[^1].Should().Be(firstSignature, "the signed string carries the previous entry's signature verbatim");
    }

    [Fact]
    public async Task CopyNumber_IsSignedOnACopyAndOmittedOtherwise()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));
        var copy = FRTestData.Copy(3);
        copy.ReceiptRequest.ftReceiptCase = ((ReceiptCase) 0x3010).WithCountry("FR");

        var (copied, _) = await sut.ProcessReceiptAsync(copy, null);
        var (ticket, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        PayloadOf(copied).Split(',')[2].Should().Be("3", "R13 TAG-DUP-PRN-NUM is part of the signed duplicate");
        PayloadOf(ticket).Split(',').Should().HaveCount(7, "a ticket signs the note/ticket string, which has no reprint number");
    }

    [Fact]
    public async Task ArchiveBoundaries_AreSignedOnAnArchiveReceiptAndOmittedOtherwise()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var (archive, _) = await sut.ProcessReceiptAsync(FRTestData.Archive(), null);
        var (ticket, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        // R13 archive string: TAG-ARC (identification, boundaries, day/month/year totals), flag, previous signature.
        var archiveFields = PayloadOf(archive).Split(',');
        archiveFields.Should().HaveCount(29);
        archiveFields[1].Should().Be(FRTestData.PreviousArchive.ToString("D"));
        PayloadOf(ticket).Should().NotContain(FRTestData.PreviousArchive.ToString("D"));
    }

    [Fact]
    public async Task ProcessReceiptAsync_AttachesTheAuditSignatures()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        var signatures = response.ReceiptResponse.ftSignatures;
        signatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.ChainHash), "ADR 0001 chains on the signature itself");
        signatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.ShiftTotals), "no separate certificate item, as in v1.2");
        var qr = signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature));
        qr.Caption.Should().Be("www.fiskaltrust.fr");
        using (var header = System.Text.Json.JsonDocument.Parse(FRTestData.FromBase64Url(qr.Data.Split('.')[0])))
        {
            header.RootElement.GetProperty("kid").GetString().Should().Be("CERT-4711", "the serial is signed in the JWS header");
        }
        signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.Siret)).Data.Should().Be(FRTestData.Siret);
        signatures.Should().ContainSingle(x => x.ftSignatureType.IsType(SignatureTypeFR.CertificationBody));
        signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).ftSignatureFormat.Should().Be(SignatureFormat.QRCode);
    }

    /// <summary>A closing prints its period and perpetual totals as readable items, as v1.2 did (market-fr#221).</summary>
    [Fact]
    public async Task Closing_PrintsTheReadablePeriodAndPerpetualTotals()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));
        var request = FRTestData.Request();
        request.PeriodTotals = new FRPeriodTotals
        {
            Period = FRTotalsPeriod.Month,
            Current = new FRTotals { Totalizer = 24.00m, CINormal = 24.00m, PICash = 24.00m },
            Perpetual = new FRTotals { Totalizer = 1000.00m, CINormal = 1000.00m, PICash = 1000.00m },
        };

        var (response, _) = await sut.ProcessReceiptAsync(request, null);

        var signatures = response.ReceiptResponse.ftSignatures;
        var month = signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.MonthTotals));
        month.Caption.Should().Be("Month Totals");
        month.Data.Should().StartWith("Total TTC: 24.00\nTVA 20 %: 24.00\n").And.Contain("Espèces: 24.00");
        signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.PerpetualTotals)).Data.Should().StartWith("Total TTC: 1000.00");
        signatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.DayTotals));
    }

    [Fact]
    public async Task Sale_PrintsNoTotalsItems()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        response.ReceiptResponse.ftSignatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.PerpetualTotals));
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
    public async Task ProcessReceiptAsync_WhenTheKeyIsGone_ReturnsTheFailureInsteadOfThrowing()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var sut = new InfoCertFRSSCD(Configuration(privateKey));
        sut.Dispose();

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        FRTestData.ShouldBeASigningFailure(response, signature);
    }

    [Fact]
    public void Constructor_WithUnreadableKey_ThrowsCertificateException()
    {
        var act = () => new InfoCertFRSSCD(Configuration(Convert.ToBase64String(new byte[] { 1, 2, 3 })));

        act.Should().Throw<FRCertificateException>().WithMessage("*secp256r1*");
    }

    [Theory]
    [InlineData("nistP384")]
    [InlineData("nistP521")]
    public void Constructor_WithAKeyOnAnotherCurve_IsRejected(string curveName)
    {
        using var otherCurveKey = ECDsa.Create(ECCurve.CreateFromFriendlyName(curveName));
        var privateKey = Convert.ToBase64String(otherCurveKey.ExportPkcs8PrivateKey());

        var act = () => new InfoCertFRSSCD(Configuration(privateKey));

        // The JWS header declares ES256 unconditionally, so any other curve would emit a signature
        // of the wrong size that no standards-compliant verifier accepts.
        act.Should().Throw<FRCertificateException>().WithMessage("*P-256*");
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

    /// <summary>
    /// R12/R13 list the seller's identity as data and archive fields, not in the signed strings: a ticket signs none of
    /// them, and neither does chain E's R19 JET string (ADR 0021).
    /// </summary>
    [Fact]
    public async Task IssuerClaims_AreInNoSignedString()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var configuration = Configuration(privateKey);
        configuration.Rcs = "RCS Nanterre B 642036065";
        configuration.Naf = "4711D";
        configuration.LegalForm = "SAS, au capital de 500000 EUR";
        configuration.VatOption = "receipts";
        configuration.ElectronicAddress = "123456789";
        using var sut = new InfoCertFRSSCD(configuration);

        var (ticket, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);
        var zero = FRTestData.Request();
        zero.ReceiptRequest.ftReceiptCase = ((ReceiptCase) 0x2000).WithCountry("FR");
        zero.ReceiptRequest.cbChargeItems = [];
        zero.ReceiptRequest.cbPayItems = [];
        var (zeroReceipt, _) = await sut.ProcessReceiptAsync(zero, null);

        PayloadOf(ticket).Should().NotContain("4711D").And.NotContain(FRTestData.Siret).And.NotContain("123456789");
        PayloadOf(zeroReceipt).Should().Be("T1,900,,20260301100000,,cashbox-fr,N,");
    }

    [Fact]
    public async Task Nf525Mention_FollowsTheReferentielFormat()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        // market-fr#131, NF525 V2.3 §6.1.6.1: characters 3, 7, 13 and 19 of the signature.
        var extract = new string(new[] { signature[2], signature[6], signature[12], signature[18] });
        var item = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.CertificationBody));
        item.Caption.Should().BeEmpty();
        item.Data.Should().Be($"(NF525) B 0546 {extract} OnQPMS 06.07");
    }

    [Fact]
    public async Task Nf525Mention_InASandbox_SaysCertificationIsPending()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var configuration = Configuration(privateKey);
        configuration.Sandbox = true;
        using var sut = new InfoCertFRSSCD(configuration);

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        // A sandbox is not certified: no attestation number, the excerpt and the software stay.
        var extract = new string(new[] { signature[2], signature[6], signature[12], signature[18] });
        var item = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.CertificationBody));
        item.Data.Should().Be($"(NF525) B en cours de certif. {extract} OnQPMS 06.07");
        item.Data.Should().NotContain("0546");
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData(null, false)]
    public void FromConfiguration_ReadsTheLauncherSandboxKey(object? sandbox, bool expected)
    {
        var configuration = new Dictionary<string, object> { ["Siret"] = FRTestData.Siret, ["PrivateKey"] = "aa", ["SoftwareCategory"] = "B", ["AttestationNumber"] = "0546", ["SoftwareVersion"] = "06.07" };
        if (sandbox is not null)
        {
            configuration["sandbox"] = sandbox;
        }

        InfoCertConfiguration.FromConfiguration(configuration).Sandbox.Should().Be(expected);
    }

    [Fact]
    public void Nf525Mention_LeavesOutAnEmptySoftwareName()
    {
        InfoCertMention.Build("C", "1234", "ABWCDExFGHIJiKLMNO2PQ", "", "v1.0").Should().Be("(NF525) C 1234 Wxi2 v1.0");
    }

    [Theory]
    [InlineData(null, "0546", "06.07")]
    [InlineData("B", "", "06.07")]
    [InlineData("B", "0546", "")]
    public void Nf525Mention_IsNotBuiltIncomplete(string? category, string productNumber, string version)
    {
        InfoCertMention.Build(category, productNumber, "ABWCDExFGHIJiKLMNO2PQ", "OnQPMS", version).Should().BeNull();
    }

    [Fact]
    public void Nf525Mention_IsNotBuiltFromATooShortSignature()
    {
        InfoCertMention.Build("B", "0546", "ABWCDExFGHIJiKLMNO", "OnQPMS", "06.07").Should().BeNull();
    }

    [Fact]
    public async Task Nf525Mention_IsOmittedWithoutACategory()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var configuration = Configuration(privateKey);
        configuration.SoftwareCategory = null;
        using var sut = new InfoCertFRSSCD(configuration);

        var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        response.ReceiptResponse.ftSignatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.CertificationBody));
    }

    /// <summary>NF525 V2.3 §6.1.6.1: outside a sandbox the InfoCert SCU refuses to start without what the mention prints.</summary>
    [Theory]
    [InlineData("SoftwareCategory")]
    [InlineData("AttestationNumber")]
    [InlineData("SoftwareVersion")]
    public void FromConfiguration_OutsideASandbox_WithoutAMentionField_IsRejected(string key)
    {
        var configuration = new Dictionary<string, object> { ["Siret"] = FRTestData.Siret, ["PrivateKey"] = "aa", ["SoftwareCategory"] = "B", ["AttestationNumber"] = "0546", ["SoftwareVersion"] = "06.07" };
        configuration.Remove(key);

        var act = () => InfoCertConfiguration.FromConfiguration(configuration);

        act.Should().Throw<FRValidationException>().WithMessage($"*{key}*");
    }

    [Fact]
    public void FromConfiguration_InASandbox_StartsWithoutAnAttestationNumber()
    {
        var configuration = new Dictionary<string, object> { ["Siret"] = FRTestData.Siret, ["PrivateKey"] = "aa", ["sandbox"] = true, ["SoftwareCategory"] = "B", ["SoftwareVersion"] = "06.07" };

        InfoCertConfiguration.FromConfiguration(configuration).Sandbox.Should().BeTrue();
    }

    /// <summary>The category and the version are known before certification: a sandbox prints the mention too.</summary>
    [Theory]
    [InlineData("SoftwareCategory")]
    [InlineData("SoftwareVersion")]
    public void FromConfiguration_InASandbox_WithoutCategoryOrVersion_IsRejected(string key)
    {
        var configuration = new Dictionary<string, object> { ["Siret"] = FRTestData.Siret, ["PrivateKey"] = "aa", ["sandbox"] = true, ["SoftwareCategory"] = "B", ["SoftwareVersion"] = "06.07" };
        configuration.Remove(key);

        var act = () => InfoCertConfiguration.FromConfiguration(configuration);

        act.Should().Throw<FRValidationException>().WithMessage($"*{key}*");
    }

    [Fact]
    public void FromConfiguration_WithAnUnknownSoftwareCategory_IsRejected()
    {
        var act = () => InfoCertConfiguration.FromConfiguration(new Dictionary<string, object> { ["Siret"] = FRTestData.Siret, ["PrivateKey"] = "aa", ["SoftwareCategory"] = "D" });

        act.Should().Throw<FRValidationException>().WithMessage("*SoftwareCategory*");
    }

    [Fact]
    public void FromConfiguration_WithAnUnknownVatOption_IsRejected()
    {
        var act = () => InfoCertConfiguration.FromConfiguration(new Dictionary<string, object> { ["Siret"] = FRTestData.Siret, ["PrivateKey"] = "aa", ["VatOption"] = "monthly" });

        act.Should().Throw<FRValidationException>().WithMessage("*VatOption*");
    }

    private static string SignatureOf(ProcessResponse response)
        => response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;

    private static byte[] FromBase64Url(string value) => FRTestData.FromBase64Url(value);

    /// <summary>The signed string: the JWS payload, Base64Url-decoded, UTF-8.</summary>
    private static string PayloadOf(ProcessResponse response) => Encoding.UTF8.GetString(FromBase64Url(SignatureOf(response).Split('.')[1]));
}
