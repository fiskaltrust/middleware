using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
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
        var dataSet = FRReceiptQrCode.SignedContentOf(signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data);
        var signature = FRChaining.ChainedSignatureOf(signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data);

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
        FRChaining.ChainedSignatureOf(signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data).Should().Be(signature);
        signatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.ChainHash), "ADR 0001 chains on the signature itself");
    }

    [Fact]
    public async Task ProcessReceiptAsync_ReturnsTheQrCode_DataSetDotSignature_WithTheCertificateSerialSigned()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        var signatures = response.ReceiptResponse.ftSignatures;
        var qr = signatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature));
        qr.ftSignatureFormat.Should().Be(SignatureFormat.QRCode, "v1.2 returned its signature as a QR code");
        qr.Caption.Should().Be("www.fiskaltrust.fr");
        qr.Data.Should().Be(FRReceiptQrCode.SignedContentOf(qr.Data) + "." + signature);
        FRReceiptQrCode.SignedContentOf(qr.Data).Split('|')[^4].Should().Be("CERT-4711", "the serial is signed in the data set, as v1.2 signed csn");
        signatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.Information), "the data set is in the QR code");
        signatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.ShiftTotals), "no separate certificate item, as in v1.2");
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
        => FRReceiptQrCode.SignedContentOf(response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data);

    [Fact]
    public async Task ProcessReceiptAsync_DataSetIsCultureInvariant()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));
        // One request for both runs: its cash box id is random and the request hash covers it.
        var receiptRequest = FRTestData.CashSaleRequest();

        var invariant = await DataSetUnderCulture(sut, CultureInfo.InvariantCulture, receiptRequest);
        // fr-FR writes decimals with a comma and would silently change every signed amount.
        var french = await DataSetUnderCulture(sut, new CultureInfo("fr-FR"), receiptRequest);

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
    public async Task ProcessReceiptAsync_WhenTheKeyIsGone_ReturnsTheFailureInsteadOfThrowing()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var sut = new LneFRSSCD(Configuration(privateKey));
        sut.Dispose();

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        FRTestData.ShouldBeASigningFailure(response, signature);
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

        var dataSet = FRReceiptQrCode.SignedContentOf(response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data);
        dataSet.Should().Contain("Day,25.50,20.00");
        dataSet.Should().Contain("99.00", "the perpetual total is reported on every closing");
    }

    [Fact]
    public void FromConfiguration_WithoutSiret_IsRejected()
    {
        var act = () => LneConfiguration.FromConfiguration(new Dictionary<string, object> { ["PrivateKey"] = "aa" });

        act.Should().Throw<FRValidationException>().WithMessage("*Siret*");
    }

    [Fact]
    public async Task IssuerClaims_FollowTheSiretInTheSignedDataSet()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var configuration = Configuration(privateKey);
        configuration.Rcs = "RCS Nanterre B 642036065";
        configuration.Naf = "4711D";
        configuration.LegalForm = "SAS, au capital de 500000 EUR";
        configuration.VatOption = "debits";
        configuration.ElectronicAddress = "123456789_POS";
        using var sut = new LneFRSSCD(configuration);

        var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        // market-fr#141: fixed positions right after the SIRET.
        DataSetOf(response).Split('|')[..6].Should().Equal(FRTestData.Siret, "RCS Nanterre B 642036065", "4711D", "SAS, au capital de 500000 EUR", "debits", "123456789_POS");
    }

    [Fact]
    public async Task IssuerClaims_AreEmptyFieldsWhenNotConfigured()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        DataSetOf(response).Split('|')[..6].Should().Equal(FRTestData.Siret, "", "", "", "", "");
    }

    [Fact]
    public async Task CertificationMention_IsTheRev18WordingWithTheLneCertificateNumber()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));

        var (response, signature) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        // market-fr#131, LNE Rev. 1.8 Exigence 9: the mention and the LNE certificate number; the
        // signature is printed in the QR code, not repeated on this line.
        var item = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.CertificationBody));
        item.Caption.Should().Be("Système de caisse certifié LNE");
        item.Data.Should().Be(Configuration(privateKey).LneCertificateNumber).And.NotBeEmpty();
        item.Data.Should().NotContain(signature);
    }

    [Fact]
    public async Task RequestHash_IsTheCanonicalRequestHashAfterTheQueueItemId()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));
        var request = FRTestData.Request();

        var (response, _) = await sut.ProcessReceiptAsync(request, null);

        // siret, five issuer claims, queue id, queue item id, then the request hash (ADR 0013).
        DataSetOf(response).Split('|')[8].Should().Be(FRCanonicalRequest.SignedField(request.ReceiptRequest)).And.StartWith("1:");
    }

    [Fact]
    public async Task CopyNumber_IsSignedAfterTheReceiptCase()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));
        var copy = FRTestData.Copy(2, asJson: false);

        var (copied, _) = await sut.ProcessReceiptAsync(copy, null);
        var (ticket, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        // ... request hash, cashbox, receipt id, moment, receipt case, then the copy number.
        DataSetOf(copied).Split('|')[13].Should().Be("2");
        DataSetOf(ticket).Split('|')[13].Should().BeEmpty("only a copy has a reprint number");
    }

    [Fact]
    public async Task SalesLineCount_IsSignedAfterTheCopyNumber_InA24FieldDataSet()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));
        var request = FRTestData.Request();
        request.ReceiptRequest.cbChargeItems =
        [
            new ChargeItem { Amount = 10m, Quantity = 1, Description = "a", ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
            new ChargeItem { Amount = 5m, Quantity = 2, Description = "b", ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
            new ChargeItem { Amount = -1m, Quantity = 1, Description = "discount", ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR").WithFlag(ChargeItemCaseFlags.ExtraOrDiscount) },
        ];
        request.ReceiptRequest.cbPayItems = [new PayItem { Amount = 14m, Description = "cash", ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }];

        var (response, _) = await sut.ProcessReceiptAsync(request, null);

        // ... receipt case, copy number, then the sales-line count (market-fr#148); the discount is not a line.
        var fields = DataSetOf(response).Split('|');
        fields[14].Should().Be("2");
        fields.Should().HaveCount(24, "the frozen LNE field order (2026-11-04)");
    }

    [Fact]
    public async Task RequestHash_ThatCannotBeComputed_ReturnsTheFailureInsteadOfThrowing()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(Configuration(privateKey));
        var request = FRTestData.Request();
        request.ReceiptRequest.cbReceiptReference = "lone \ud800 surrogate";

        var (response, signature) = await sut.ProcessReceiptAsync(request, null);

        FRTestData.ShouldBeASigningFailure(response, signature);
    }

    [Fact]
    public void FromConfiguration_WithAnUnknownVatOption_IsRejected()
    {
        var act = () => LneConfiguration.FromConfiguration(new Dictionary<string, object> { ["Siret"] = FRTestData.Siret, ["PrivateKey"] = "aa", ["VatOption"] = "monthly" });

        act.Should().Throw<FRValidationException>().WithMessage("*VatOption*");
    }

    [Fact]
    public void FromConfiguration_WithTheFieldSeparatorInAClaim_IsRejected()
    {
        var act = () => LneConfiguration.FromConfiguration(new Dictionary<string, object> { ["Siret"] = FRTestData.Siret, ["PrivateKey"] = "aa", ["LegalForm"] = "SAS|SARL" });

        act.Should().Throw<FRValidationException>().WithMessage("*LegalForm*|*");
    }

    [Fact]
    public void FromConfiguration_ReadsTheIssuerClaims()
    {
        var configuration = LneConfiguration.FromConfiguration(new Dictionary<string, object> { ["Siret"] = FRTestData.Siret, ["PrivateKey"] = "aa", ["rcs"] = "RCS Paris B 1", ["VatOption"] = "receipts" });

        configuration.Rcs.Should().Be("RCS Paris B 1");
        configuration.VatOption.Should().Be("receipts");
    }

    private static async Task<string> DataSetUnderCulture(LneFRSSCD sut, CultureInfo culture, fiskaltrust.ifPOS.v2.ReceiptRequest receiptRequest)
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = culture;
        try
        {
            var request = FRTestData.Request();
            request.ReceiptRequest = receiptRequest;
            var (response, _) = await sut.ProcessReceiptAsync(request, null);
            return FRReceiptQrCode.SignedContentOf(response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
