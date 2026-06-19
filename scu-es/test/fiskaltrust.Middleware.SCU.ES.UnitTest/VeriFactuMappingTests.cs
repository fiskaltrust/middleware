using System;
using System.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.ES.VeriFactu;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Mapping;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.VeriFactuUnitTest;

public class VeriFactuMappingTests
{
    private const ulong EsServiceFlag = 0x4752_2000_0000_0000;

    public static TheoryData<ChargeItemCaseNatureOfVatES, Impuesto, IdOperacionesTrascendenciaTributaria, object> NatureOfVatTuples()
    {
        // (nature, L1 Impuesto, L8A ClaveRegimen, L9 CalificacionOperacion | L10 OperacionExenta)
        // Regime: MainlandVat (L1 = 01) for all rows in this matrix.
        return new TheoryData<ChargeItemCaseNatureOfVatES, Impuesto, IdOperacionesTrascendenciaTributaria, object>
        {
            { ChargeItemCaseNatureOfVatES.UsualVatApplies,           Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item01, CalificacionOperacion.S1 }, // NN [00]
            { ChargeItemCaseNatureOfVatES.NotSubjectLocationRules,   Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item01, CalificacionOperacion.N2 }, // NN [20]
            { ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14,   Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item01, CalificacionOperacion.N1 }, // NN [21]
            { ChargeItemCaseNatureOfVatES.Exports,                   Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item02, OperacionExenta.E2 },        // NN [10]
            { ChargeItemCaseNatureOfVatES.IntraCommunityDelivery,    Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item01, OperacionExenta.E5 },        // NN [11]
            { ChargeItemCaseNatureOfVatES.TransactionsTreatedAsExports, Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item02, OperacionExenta.E3 },     // NN [13]
            { ChargeItemCaseNatureOfVatES.CustomsAndTaxExemptions,   Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item02, OperacionExenta.E4 },        // NN [14]
            { ChargeItemCaseNatureOfVatES.ExemptedDomestic,          Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item01, OperacionExenta.E1 },        // NN [30]
            { ChargeItemCaseNatureOfVatES.OtherExemptions,           Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item01, OperacionExenta.E6 },        // NN [31]
            { ChargeItemCaseNatureOfVatES.ReverseCharge,             Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item01, CalificacionOperacion.S2 }, // NN [50]
            // NN[60] / NN[80] have no dedicated L9/L10 key in VeriFactu; mapper routes to N1.
            { ChargeItemCaseNatureOfVatES.ForeignTaxApplies,         Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item01, CalificacionOperacion.N1 }, // NN [60]
            { ChargeItemCaseNatureOfVatES.ExcludedThirdParty,        Impuesto.Item01, IdOperacionesTrascendenciaTributaria.Item01, CalificacionOperacion.N1 }, // NN [80]
        };
    }

    [Theory]
    [MemberData(nameof(NatureOfVatTuples))]
    public void CreateRegistroFacturacionAlta_MapsNatureOfVat_ToCorrectVeriFactuTuple(
        ChargeItemCaseNatureOfVatES nature,
        Impuesto expectedImpuesto,
        IdOperacionesTrascendenciaTributaria expectedClaveRegimen,
        object expectedItem)
    {
        var mapping = new VeriFactuMapping(MainlandVatConfig(), signXml: false);
        var (request, response) = BuildRequestResponseForNature(nature);

        var alta = mapping.CreateRegistroFacturacionAlta(request, response, null, null);

        alta.Desglose.Should().HaveCount(1);
        var detalle = alta.Desglose[0];
        detalle.Impuesto.Should().Be(expectedImpuesto);
        detalle.ClaveRegimen.Should().Be(expectedClaveRegimen);
        detalle.Item.Should().Be(expectedItem);
    }

    [Theory]
    [InlineData(VeriFactuTaxRegime.MainlandVat, Impuesto.Item01)]
    [InlineData(VeriFactuTaxRegime.IPSI, Impuesto.Item02)]
    [InlineData(VeriFactuTaxRegime.IGIC, Impuesto.Item03)]
    [InlineData(VeriFactuTaxRegime.Other, Impuesto.Item05)]
    public void CreateRegistroFacturacionAlta_DerivesImpuesto_FromTaxRegime(VeriFactuTaxRegime regime, Impuesto expected)
    {
        var config = MainlandVatConfig();
        config.TaxRegime = regime;
        var mapping = new VeriFactuMapping(config, signXml: false);
        var (request, response) = BuildRequestResponseForNature(ChargeItemCaseNatureOfVatES.UsualVatApplies);

        var alta = mapping.CreateRegistroFacturacionAlta(request, response, null, null);

        alta.Desglose[0].Impuesto.Should().Be(expected);
    }

    [Fact]
    public void CreateRegistroFacturacionAlta_DefaultsToMainlandVat_WhenRegimeNotSet()
    {
        var mapping = new VeriFactuMapping(MainlandVatConfig(), signXml: false);
        var (request, response) = BuildRequestResponseForNature(ChargeItemCaseNatureOfVatES.UsualVatApplies);

        var alta = mapping.CreateRegistroFacturacionAlta(request, response, null, null);

        alta.Desglose[0].Impuesto.Should().Be(Impuesto.Item01);
    }

    private static VeriFactuSCUConfiguration MainlandVatConfig() => new()
    {
        Nif = "M0291081Q",
        NombreRazonEmisor = "Test Emisor",
        // Certificate intentionally null; signXml: false skips signing.
        TaxRegime = VeriFactuTaxRegime.MainlandVat,
    };

    private static (ReceiptRequest, ReceiptResponse) BuildRequestResponseForNature(ChargeItemCaseNatureOfVatES nature)
    {
        var isExempt = nature != ChargeItemCaseNatureOfVatES.UsualVatApplies;
        var vatRate = isExempt ? 0m : 21m;
        var amount = isExempt ? 10m : 12.10m;
        var vatAmount = isExempt ? 0m : 2.10m;

        var chargeItemCase = (ChargeItemCase) (EsServiceFlag | (ulong) nature);

        var request = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftReceiptCase = (ReceiptCase) EsServiceFlag,
            cbTerminalID = "1",
            cbReceiptReference = Guid.NewGuid().ToString(),
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems =
            [
                new ChargeItem
                {
                    Position = 1,
                    ftChargeItemCase = chargeItemCase,
                    VATAmount = vatAmount,
                    Amount = amount,
                    VATRate = vatRate,
                    Quantity = 1,
                    Description = "ChargeItem"
                }
            ],
            cbPayItems =
            [
                new PayItem
                {
                    ftPayItemCase = (PayItemCase) (EsServiceFlag | 0x0001),
                    Amount = amount,
                    Description = "Cash"
                }
            ]
        };

        var response = new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftQueueRow = 0,
            ftCashBoxIdentification = Guid.NewGuid().ToString(),
            ftReceiptIdentification = $"0#0/{request.cbReceiptReference}",
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) EsServiceFlag,
        };

        return (request, response);
    }
}
