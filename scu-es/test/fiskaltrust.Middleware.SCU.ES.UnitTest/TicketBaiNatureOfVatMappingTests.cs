using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.UnitTest;

public class TicketBaiNatureOfVatMappingTests
{
    [Fact]
    public void Map_UsualVatApplies_ReturnsNoExentaS1()
    {
        var result = TicketBaiNatureOfVatMapping.Map(ChargeItemCaseNatureOfVatES.UsualVatApplies);

        result.ClaveRegimen.Should().Be(IdOperacionesTrascendenciaTributariaType.Item01);
        result.Branch.Should().Be(DesgloseBranch.NoExenta);
        result.TipoNoExenta.Should().Be(TipoOperacionSujetaNoExentaType.S1);
        result.CausaExencion.Should().BeNull();
        result.CausaNoSujeta.Should().BeNull();
    }

    [Fact]
    public void Map_ReverseCharge_ReturnsNoExentaS2()
    {
        var result = TicketBaiNatureOfVatMapping.Map(ChargeItemCaseNatureOfVatES.ReverseCharge);

        result.ClaveRegimen.Should().Be(IdOperacionesTrascendenciaTributariaType.Item01);
        result.Branch.Should().Be(DesgloseBranch.NoExenta);
        result.TipoNoExenta.Should().Be(TipoOperacionSujetaNoExentaType.S2);
    }

    [Theory]
    [InlineData(ChargeItemCaseNatureOfVatES.ExemptedDomestic, CausaExencionType.E1, IdOperacionesTrascendenciaTributariaType.Item01)]            // NN [30]
    [InlineData(ChargeItemCaseNatureOfVatES.Exports, CausaExencionType.E2, IdOperacionesTrascendenciaTributariaType.Item02)]                    // NN [10]
    [InlineData(ChargeItemCaseNatureOfVatES.TransactionsTreatedAsExports, CausaExencionType.E3, IdOperacionesTrascendenciaTributariaType.Item02)] // NN [13]
    [InlineData(ChargeItemCaseNatureOfVatES.CustomsAndTaxExemptions, CausaExencionType.E4, IdOperacionesTrascendenciaTributariaType.Item02)]     // NN [14]
    [InlineData(ChargeItemCaseNatureOfVatES.IntraCommunityDelivery, CausaExencionType.E5, IdOperacionesTrascendenciaTributariaType.Item01)]      // NN [11]
    [InlineData(ChargeItemCaseNatureOfVatES.OtherExemptions, CausaExencionType.E6, IdOperacionesTrascendenciaTributariaType.Item01)]             // NN [31]
    public void Map_ExemptNatures_ReturnExentaWithExpectedCausa(
        ChargeItemCaseNatureOfVatES nature,
        CausaExencionType expectedCausa,
        IdOperacionesTrascendenciaTributariaType expectedClave)
    {
        var result = TicketBaiNatureOfVatMapping.Map(nature);

        result.Branch.Should().Be(DesgloseBranch.Exenta);
        result.CausaExencion.Should().Be(expectedCausa);
        result.ClaveRegimen.Should().Be(expectedClave);
        result.TipoNoExenta.Should().BeNull();
        result.CausaNoSujeta.Should().BeNull();
    }

    [Theory]
    [InlineData(ChargeItemCaseNatureOfVatES.NotSubjectLocationRules, CausaNoSujetaType.RL, IdOperacionesTrascendenciaTributariaType.Item01)]  // NN [20]
    [InlineData(ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14, CausaNoSujetaType.OT, IdOperacionesTrascendenciaTributariaType.Item01)]  // NN [21]
    [InlineData(ChargeItemCaseNatureOfVatES.ForeignTaxApplies, CausaNoSujetaType.IE, IdOperacionesTrascendenciaTributariaType.Item08)]        // NN [60]
    [InlineData(ChargeItemCaseNatureOfVatES.ExcludedThirdParty, CausaNoSujetaType.VT, IdOperacionesTrascendenciaTributariaType.Item01)]       // NN [80]
    public void Map_NotSubjectNatures_ReturnNoSujetaWithExpectedCausa(
        ChargeItemCaseNatureOfVatES nature,
        CausaNoSujetaType expectedCausa,
        IdOperacionesTrascendenciaTributariaType expectedClave)
    {
        var result = TicketBaiNatureOfVatMapping.Map(nature);

        result.Branch.Should().Be(DesgloseBranch.NoSujeta);
        result.CausaNoSujeta.Should().Be(expectedCausa);
        result.ClaveRegimen.Should().Be(expectedClave);
        result.CausaExencion.Should().BeNull();
        result.TipoNoExenta.Should().BeNull();
    }
}
