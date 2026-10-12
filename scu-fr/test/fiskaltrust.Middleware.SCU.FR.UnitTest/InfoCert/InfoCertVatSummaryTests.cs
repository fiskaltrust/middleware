using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;
using fiskaltrust.Middleware.SCU.FR.LNE;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest.InfoCert;

public class InfoCertVatSummaryTests
{
    private static ChargeItem Item(decimal amount, decimal rate, decimal? vat = null, ChargeItemCase vatCase = ChargeItemCase.NormalVatRate)
        => new() { Amount = amount, VATRate = rate, VATAmount = vat, Quantity = 1, Description = "item", ftChargeItemCase = vatCase.WithCountry("FR") };

    private static ReceiptRequest Request(params ChargeItem[] items)
    {
        var request = FRTestData.CashSaleRequest();
        request.cbChargeItems = items.ToList();
        return request;
    }

    [Fact]
    public void From_GroupsTheChargeItemsByRate()
    {
        var summary = InfoCertVatSummary.From(FRTestData.CashSaleRequest())!;

        summary.Keys.Should().Equal("20", "5.5");
        summary["20"].Should().BeEquivalentTo(new FRVatRateSummary { AmountHT = 10.00m, VATAmount = 2.00m, AmountTTC = 12.00m });
        summary["5.5"].Should().BeEquivalentTo(new FRVatRateSummary { AmountHT = 5.21m, VATAmount = 0.29m, AmountTTC = 5.50m });
    }

    [Fact]
    public void From_SumsTheLinesOfTheSameRate()
    {
        var summary = InfoCertVatSummary.From(Request(Item(12.00m, 20m, 2.00m), Item(6.00m, 20m, 1.00m), Item(-1.20m, 20m, -0.20m)))!;

        summary.Should().ContainSingle();
        summary["20"].Should().BeEquivalentTo(new FRVatRateSummary { AmountHT = 14.00m, VATAmount = 2.80m, AmountTTC = 16.80m }, "discounts and refunds reduce the rate's totals");
    }

    [Fact]
    public void From_DerivesTheVatFromTheGrossAmountWhenTheItemCarriesNone()
    {
        var summary = InfoCertVatSummary.From(Request(Item(10.00m, 5.5m), Item(10.00m, 5.5m)))!;

        // 10.00 / 105.5 * 5.5 = 0.52132..., twice = 1.04265... -> 1.04: the sum is rounded once per rate.
        summary["5.5"].Should().BeEquivalentTo(new FRVatRateSummary { AmountHT = 18.96m, VATAmount = 1.04m, AmountTTC = 20.00m });
    }

    [Fact]
    public void From_KeepsTheZeroRateAsItsOwnLine()
    {
        var summary = InfoCertVatSummary.From(Request(Item(3.00m, 0m, vatCase: ChargeItemCase.ZeroVatRate), Item(12.00m, 20m, 2.00m)))!;

        summary.Keys.Should().Equal("20", "0");
        summary["0"].Should().BeEquivalentTo(new FRVatRateSummary { AmountHT = 3.00m, VATAmount = 0m, AmountTTC = 3.00m });
    }

    [Fact]
    public void From_ReturnsNullForAReceiptWithoutChargeItems()
    {
        InfoCertVatSummary.From(Request()).Should().BeNull();
    }

    [Theory]
    [InlineData("20", "20")]
    [InlineData("20.00", "20")]
    [InlineData("5.5", "5.5")]
    [InlineData("5.50", "5.5")]
    [InlineData("2.1", "2.1")]
    [InlineData("0", "0")]
    public void RateKey_IsCultureInvariantWithoutTrailingZeros(string rate, string expected)
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        try
        {
            InfoCertVatSummary.RateKey(decimal.Parse(rate, CultureInfo.InvariantCulture)).Should().Be(expected);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task ProcessReceiptAsync_ReturnsTheSummaryWithTheSignedReceipt()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(new InfoCertConfiguration
        {
            Siret = FRTestData.Siret,
            PrivateKey = privateKey,
            CertificateSerialNumber = "CERT-4711",
            AttestationNumber = "IC-2026-001",
            SoftwareVersion = "1.0.0",
        });

        var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        response.VatSummary.Should().NotBeNull();
        response.VatSummary!.Keys.Should().Equal("20", "5.5");
    }

    [Fact]
    public async Task ProcessReceiptAsync_LneProducesNoSummary()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new LneFRSSCD(new LneConfiguration
        {
            Siret = FRTestData.Siret,
            PrivateKey = privateKey,
            CertificateSerialNumber = "CERT-4711",
            LneCertificateNumber = "LNE-2026-042",
            SoftwareVersion = "1.0.0",
        });

        var (response, _) = await sut.ProcessReceiptAsync(FRTestData.Request(), null);

        response.VatSummary.Should().BeNull("only the InfoCert SCU produces the VAT summary (ADR 0004)");
    }
}
