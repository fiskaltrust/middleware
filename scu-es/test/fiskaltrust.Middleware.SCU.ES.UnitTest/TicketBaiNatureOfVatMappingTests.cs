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
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptArticle20, CausaExencionType.E1, IdOperacionesTrascendenciaTributariaType.Item01)]
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptArticle21, CausaExencionType.E2, IdOperacionesTrascendenciaTributariaType.Item02)]
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptArticle22, CausaExencionType.E3, IdOperacionesTrascendenciaTributariaType.Item02)]
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptArticle23And24, CausaExencionType.E4, IdOperacionesTrascendenciaTributariaType.Item02)]
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptArticle25, CausaExencionType.E5, IdOperacionesTrascendenciaTributariaType.Item01)]
    [InlineData(ChargeItemCaseNatureOfVatES.ExteptOthers, CausaExencionType.E6, IdOperacionesTrascendenciaTributariaType.Item01)]
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
    [InlineData(ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14, CausaNoSujetaType.OT)]
    [InlineData(ChargeItemCaseNatureOfVatES.NotSubjectLocationRules, CausaNoSujetaType.RL)]
    public void Map_NotSubjectNatures_ReturnNoSujetaWithExpectedCausa(
        ChargeItemCaseNatureOfVatES nature,
        CausaNoSujetaType expectedCausa)
    {
        var result = TicketBaiNatureOfVatMapping.Map(nature);

        result.Branch.Should().Be(DesgloseBranch.NoSujeta);
        result.CausaNoSujeta.Should().Be(expectedCausa);
        result.ClaveRegimen.Should().Be(IdOperacionesTrascendenciaTributariaType.Item01);
        result.CausaExencion.Should().BeNull();
        result.TipoNoExenta.Should().BeNull();
    }
}
