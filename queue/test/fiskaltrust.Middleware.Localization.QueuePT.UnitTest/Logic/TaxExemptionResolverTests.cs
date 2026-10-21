using System.Linq;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Logic;
using fiskaltrust.Middleware.Localization.QueuePT.Logic.Exports.SAFTPT.SAFTSchemaPT10401;
using fiskaltrust.Middleware.Localization.QueuePT.Validation;
using fiskaltrust.Middleware.Localization.QueuePT.Validation.Rules;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.Logic;

public class TaxExemptionResolverTests
{
    // NotTaxable (V=8), Delivery (S=1)
    private const long NotTaxableNoNature = 0x5054_2000_0000_0018;
    private const long NotTaxableM06 = 0x5054_2000_0000_0618;
    private const long NotTaxableNature30 = 0x5054_2000_0000_3018;
    private const long NotTaxableNature80 = 0x5054_2000_0000_8018;
    private const long NormalVat = 0x5054_2000_0000_0013;

    private static ChargeItem CreateChargeItem(long chargeItemCase, decimal vatRate = 0m, object? caseData = null) => new()
    {
        Position = 1,
        Description = "Transporte internacional",
        Quantity = 1,
        Amount = 50,
        VATRate = vatRate,
        ftChargeItemCase = (ChargeItemCase) chargeItemCase,
        ftChargeItemCaseData = caseData
    };

    private static object OverrideAsJsonElement(string code) => JsonSerializer.Deserialize<JsonElement>($$"""{ "PT": { "TaxExemptionCode": "{{code}}" } }""");

    [Fact]
    public void TryResolve_WithoutOverride_ReadsCodeFromNatureOfVat()
    {
        var chargeItem = CreateChargeItem(NotTaxableM06);

        TaxExemptionResolver.TryResolve(chargeItem, out var code).Should().BeTrue();
        code.Should().Be(Constants.TaxExemptionCode.M06);
    }

    [Fact]
    public void TryResolve_WithOverride_TakesPrecedenceOverNatureOfVat()
    {
        // Without the override NN 0x30 resolves to M30
        var chargeItem = CreateChargeItem(NotTaxableNature30, caseData: OverrideAsJsonElement("M06"));

        TaxExemptionResolver.TryResolve(chargeItem, out var code).Should().BeTrue();
        code.Should().Be(Constants.TaxExemptionCode.M06);
    }

    [Fact]
    public void TryResolve_WithOverrideAsJsonString_IsSupported()
    {
        var chargeItem = CreateChargeItem(NotTaxableNature30, caseData: """{ "PT": { "TaxExemptionCode": "M06" } }""");

        TaxExemptionResolver.TryResolve(chargeItem, out var code).Should().BeTrue();
        code.Should().Be(Constants.TaxExemptionCode.M06);
    }

    [Fact]
    public void TryResolve_WithOverrideAsObject_IsSupported()
    {
        var chargeItem = CreateChargeItem(NotTaxableNature30, caseData: new { PT = new { TaxExemptionCode = "M06" } });

        TaxExemptionResolver.TryResolve(chargeItem, out var code).Should().BeTrue();
        code.Should().Be(Constants.TaxExemptionCode.M06);
    }

    [Theory]
    [InlineData("m06")]
    [InlineData(" M06 ")]
    public void TryResolve_WithOverride_IgnoresCaseAndWhitespace(string value)
    {
        var chargeItem = CreateChargeItem(NotTaxableNature80, caseData: OverrideAsJsonElement(value));

        TaxExemptionResolver.TryResolve(chargeItem, out var code).Should().BeTrue();
        code.Should().Be(Constants.TaxExemptionCode.M06);
    }

    [Theory]
    [InlineData("M03")]
    [InlineData("M6")]
    [InlineData("X06")]
    [InlineData("M100")]
    [InlineData("")]
    public void TryResolve_WithInvalidOverride_FailsWithoutFallingBackToNatureOfVat(string value)
    {
        var chargeItem = CreateChargeItem(NotTaxableM06, caseData: OverrideAsJsonElement(value));

        TaxExemptionResolver.TryResolve(chargeItem, out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetTaxExemptionCodeOverride_WithOtherCaseData_ReturnsFalse()
    {
        var chargeItem = CreateChargeItem(NotTaxableM06, caseData: JsonSerializer.Deserialize<JsonElement>("""{ "GR": { "Foo": "Bar" } }"""));

        TaxExemptionResolver.TryGetTaxExemptionCodeOverride(chargeItem, out _).Should().BeFalse();
        TaxExemptionResolver.TryResolve(chargeItem, out var code).Should().BeTrue();
        code.Should().Be(Constants.TaxExemptionCode.M06);
    }

    [Fact]
    public void PTMappings_WithOverride_ReturnsOverriddenCodeAndReason()
    {
        var chargeItem = CreateChargeItem(NotTaxableNature30, caseData: OverrideAsJsonElement("M06"));

        PTMappings.GetTaxExemptionCode(chargeItem).Should().Be("M06");
        PTMappings.GetTaxExemptionReason(chargeItem).Should().Be("Isento artigo 15.º do CIVA");
    }

    [Fact]
    public void PTMappings_WithoutOverride_ReturnsCodeFromNatureOfVat()
    {
        var chargeItem = CreateChargeItem(NotTaxableNature30);

        PTMappings.GetTaxExemptionCode(chargeItem).Should().Be("M30");
        PTMappings.GetTaxExemptionReason(chargeItem).Should().Be("IVA - autoliquidação");
    }

    [Fact]
    public void Validation_ZeroVatWithValidOverrideAndGeneralNatureOfVat_Passes()
    {
        // NN 0x80 (Excluded) is not an M-code, the override provides the exact code
        var request = CreateRequest(CreateChargeItem(NotTaxableNature80, caseData: OverrideAsJsonElement("M99")));

        ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).Should().BeEmpty();
    }

    [Fact]
    public void Validation_ZeroVatWithUnknownOverride_Fails()
    {
        var request = CreateRequest(CreateChargeItem(NotTaxableM06, caseData: OverrideAsJsonElement("M03")));

        var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

        results.Should().ContainSingle();
        var error = results[0].Errors.Single();
        error.Code.Should().Be(PortugalValidationRules.UnknownTaxExemptionCode.Code);
        error.Field.Should().Be(PortugalValidationFields.ChargeItemsCaseData);
        error.Message.Should().Contain("'M03'").And.Contain("ftChargeItemCaseData.PT.TaxExemptionCode");
    }

    [Fact]
    public void Validation_ZeroVatWithOverrideButWithoutNatureOfVat_Fails()
    {
        var request = CreateRequest(CreateChargeItem(NotTaxableNoNature, caseData: OverrideAsJsonElement("M06")));

        var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

        results.Should().ContainSingle();
        results[0].Errors.Single().Code.Should().Be(PortugalValidationRules.ZeroVatRateMissingNature.Code);
    }

    [Fact]
    public void Validation_TaxedLineWithOverride_Fails()
    {
        var request = CreateRequest(CreateChargeItem(NormalVat, vatRate: 23m, caseData: OverrideAsJsonElement("M06")));

        var results = ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).ToList();

        results.Should().ContainSingle();
        results[0].Errors.Single().Code.Should().Be(PortugalValidationRules.TaxExemptionCodeOnTaxedLine.Code);
    }

    [Fact]
    public void Validation_ZeroVatWithNatureOfVatOnly_StillPasses()
    {
        var request = CreateRequest(CreateChargeItem(NotTaxableM06));

        ChargeItemValidations.Validate_ChargeItems_VATRate_ZeroVatRateNature(request).Should().BeEmpty();
    }

    private static ReceiptRequest CreateRequest(ChargeItem chargeItem) => new()
    {
        ftReceiptCase = ReceiptCase.InvoiceB2B0x1002.WithCountry("PT"),
        cbChargeItems = [chargeItem],
        cbPayItems = []
    };
}
