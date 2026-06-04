using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Validation.Rules;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.Validation;

/// <summary>
/// Covers the legacy (currently authoritative) QueueES zero-VAT nature rule after the B1 change:
/// a 0% line must carry *some* exemption / not-subject nature, but the queue no longer rejects the
/// *specific* code — that is left to the SCU (VeriFactu / TicketBAI mappers). Previously every
/// non-usual nature was rejected with EEEE_UnknownTaxExemptionCode.
/// </summary>
public class ESLegacyZeroVatNatureTests
{
    private static ReceiptRequest RequestWith(ChargeItemCaseNatureOfVatES nature, decimal vatRate) => new()
    {
        ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("ES"),
        cbChargeItems = [new ChargeItem
        {
            Description = "Item",
            VATRate = vatRate,
            Amount = 89.36m,
            VATAmount = 0m,
            ftChargeItemCase = (ChargeItemCase) ((long) ChargeItemCase.ZeroVatRate.WithCountry("ES") | (long) nature)
        }],
        cbPayItems = []
    };

    [Theory]
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptArticle20)]          // NN[30] E1
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptArticle21)]          // NN[10] E2 — was rejected before B1
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptArticle22)]          // NN[13] E3 — was rejected before B1
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptArticle23And24)]    // NN[14] E4 — was rejected before B1
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptArticle25)]          // NN[11] E5
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptOthers)]             // NN[31] E6 — was rejected before B1
    [InlineData(ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14)] // NN[21] OT
    [InlineData(ChargeItemCaseNatureOfVatES.NotSubjectLocationRules)] // NN[20] RL
    [InlineData(ChargeItemCaseNatureOfVatES.ReverseCharge)]           // NN[50] S2
    public void ZeroVatRate_WithExemptNature_IsAccepted(ChargeItemCaseNatureOfVatES nature)
    {
        var results = ChargeItemValidations
            .Validate_ChargeItems_VATRate_ZeroVatRateNature(RequestWith(nature, 0m))
            .ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void ZeroVatRate_WithUsualVatApplies_IsRejectedAsMissingNature()
    {
        var results = ChargeItemValidations
            .Validate_ChargeItems_VATRate_ZeroVatRateNature(RequestWith(ChargeItemCaseNatureOfVatES.UsualVatApplies, 0m))
            .ToList();

        var result = Assert.Single(results);
        Assert.Equal("EEEE_ZeroVatRateMissingNature", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void NonZeroVatRate_WithoutNature_IsAccepted()
    {
        // Non-zero VAT lines do not require a nature; the rule must not flag them.
        var results = ChargeItemValidations
            .Validate_ChargeItems_VATRate_ZeroVatRateNature(RequestWith(ChargeItemCaseNatureOfVatES.UsualVatApplies, 21m))
            .ToList();

        Assert.Empty(results);
    }
}
