using System;
using System.Collections.Generic;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.SCU.ES.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.UnitTest;

public class TicketBaiFactoryTests
{
    private static TicketBaiSCUConfiguration BuildConfig() => new()
    {
        EmisorNif = "B10646545",
        EmisorApellidosNombreRazonSocial = "TEST EMISOR",
        SoftwareVersion = "1.0",
        SoftwareName = "TestSoftware",
        SoftwareLicenciaTBAI = "TBAITEST0000000000001",
        SoftwareNif = "B10646545"
    };

    private static ChargeItemCase WithNature(ChargeItemCaseNatureOfVatES nature) =>
        (ChargeItemCase)(long) nature;

    private static ChargeItem ChargeItem(decimal amount, decimal vatRate, decimal vatAmount, ChargeItemCaseNatureOfVatES nature) => new()
    {
        VATRate = vatRate,
        Amount = amount,
        VATAmount = vatAmount,
        Description = "item",
        Quantity = 1,
        ftChargeItemCase = WithNature(nature)
    };

    private static ProcessRequest BuildRequest(IEnumerable<ChargeItem> chargeItems, object customer = null)
    {
        var request = new ReceiptRequest
        {
            cbReceiptReference = "ref-1",
            cbReceiptMoment = DateTime.UtcNow,
            cbChargeItems = [.. chargeItems],
            cbCustomer = customer
        };
        // Round-trip through JSON so ftStateData ends up as a JsonElement, mirroring how a real
        // ProcessRequest reaches the factory.
        return JsonSerializer.Deserialize<ProcessRequest>(JsonSerializer.Serialize(new ProcessRequest
        {
            ReceiptRequest = request,
            ReceiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "0#TEST/123",
                ftReceiptMoment = DateTime.UtcNow,
                ftCashBoxIdentification = "test-cashbox",
                ftStateData = new MiddlewareStateData { ES = new MiddlewareStateDataES() }
            }
        }))!;
    }

    [Fact]
    public void ConvertTo_DomesticUsualVat_ProducesSujetaNoExentaS1()
    {
        var factory = new TicketBaiFactory(BuildConfig());

        var ticketBai = factory.ConvertTo(BuildRequest([
            ChargeItem(amount: 121m, vatRate: 21m, vatAmount: 21m, ChargeItemCaseNatureOfVatES.UsualVatApplies)
        ]));

        var desglose = ticketBai.Factura.TipoDesglose.Item.Should().BeOfType<DesgloseFacturaType>().Subject;
        desglose.NoSujeta.Should().BeNull();
        desglose.Sujeta.Exenta.Should().BeNull();
        desglose.Sujeta.NoExenta.Should().HaveCount(1);
        var noExenta = desglose.Sujeta.NoExenta[0];
        noExenta.TipoNoExenta.Should().Be(TipoOperacionSujetaNoExentaType.S1);
        noExenta.DesgloseIVA.Should().HaveCount(1);
        noExenta.DesgloseIVA[0].BaseImponible.Should().Be("100.00");
        noExenta.DesgloseIVA[0].TipoImpositivo.Should().Be("21.00");
        noExenta.DesgloseIVA[0].CuotaImpuesto.Should().Be("21.00");
    }

    [Fact]
    public void ConvertTo_DomesticReverseCharge_ProducesSujetaNoExentaS2()
    {
        var factory = new TicketBaiFactory(BuildConfig());

        var ticketBai = factory.ConvertTo(BuildRequest([
            ChargeItem(amount: 100m, vatRate: 0m, vatAmount: 0m, ChargeItemCaseNatureOfVatES.ReverseCharge)
        ]));

        var desglose = ticketBai.Factura.TipoDesglose.Item.Should().BeOfType<DesgloseFacturaType>().Subject;
        desglose.Sujeta.NoExenta.Should().HaveCount(1);
        desglose.Sujeta.NoExenta[0].TipoNoExenta.Should().Be(TipoOperacionSujetaNoExentaType.S2);
    }

    [Fact]
    public void ConvertTo_DomesticExportExempt_ProducesSujetaExentaWithE2AndClaveItem02()
    {
        var factory = new TicketBaiFactory(BuildConfig());

        var ticketBai = factory.ConvertTo(BuildRequest([
            ChargeItem(amount: 50m, vatRate: 0m, vatAmount: 0m, ChargeItemCaseNatureOfVatES.ExteptArticle21)
        ]));

        var desglose = ticketBai.Factura.TipoDesglose.Item.Should().BeOfType<DesgloseFacturaType>().Subject;
        desglose.NoSujeta.Should().BeNull();
        desglose.Sujeta.NoExenta.Should().BeNull();
        desglose.Sujeta.Exenta.Should().HaveCount(1);
        desglose.Sujeta.Exenta[0].CausaExencion.Should().Be(CausaExencionType.E2);
        desglose.Sujeta.Exenta[0].BaseImponible.Should().Be("50.00");

        ticketBai.Factura.DatosFactura.Claves.Should().HaveCount(1);
        ticketBai.Factura.DatosFactura.Claves[0].ClaveRegimenIvaOpTrascendencia
            .Should().Be(IdOperacionesTrascendenciaTributariaType.Item02);
    }

    [Fact]
    public void ConvertTo_DomesticNotSubjectLocationRules_ProducesNoSujetaRL()
    {
        var factory = new TicketBaiFactory(BuildConfig());

        var ticketBai = factory.ConvertTo(BuildRequest([
            ChargeItem(amount: 80m, vatRate: 0m, vatAmount: 0m, ChargeItemCaseNatureOfVatES.NotSubjectLocationRules)
        ]));

        var desglose = ticketBai.Factura.TipoDesglose.Item.Should().BeOfType<DesgloseFacturaType>().Subject;
        desglose.Sujeta.Should().BeNull();
        desglose.NoSujeta.Should().HaveCount(1);
        desglose.NoSujeta[0].Causa.Should().Be(CausaNoSujetaType.RL);
        desglose.NoSujeta[0].Importe.Should().Be("80.00");
    }

    [Fact]
    public void ConvertTo_DomesticMixed_GroupsByBranchAndProducesAllSections()
    {
        var factory = new TicketBaiFactory(BuildConfig());

        var ticketBai = factory.ConvertTo(BuildRequest([
            ChargeItem(amount: 121m, vatRate: 21m, vatAmount: 21m, ChargeItemCaseNatureOfVatES.UsualVatApplies),
            ChargeItem(amount: 110m, vatRate: 10m, vatAmount: 10m, ChargeItemCaseNatureOfVatES.UsualVatApplies),
            ChargeItem(amount: 100m, vatRate: 0m, vatAmount: 0m, ChargeItemCaseNatureOfVatES.ReverseCharge),
            ChargeItem(amount: 50m, vatRate: 0m, vatAmount: 0m, ChargeItemCaseNatureOfVatES.ExteptArticle20),
            ChargeItem(amount: 30m, vatRate: 0m, vatAmount: 0m, ChargeItemCaseNatureOfVatES.ExteptArticle25),
            ChargeItem(amount: 80m, vatRate: 0m, vatAmount: 0m, ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14),
            ChargeItem(amount: 20m, vatRate: 0m, vatAmount: 0m, ChargeItemCaseNatureOfVatES.NotSubjectLocationRules)
        ]));

        var desglose = ticketBai.Factura.TipoDesglose.Item.Should().BeOfType<DesgloseFacturaType>().Subject;

        desglose.Sujeta.Exenta.Should().HaveCount(2);
        desglose.Sujeta.Exenta.Should().Contain(e => e.CausaExencion == CausaExencionType.E1 && e.BaseImponible == "50.00");
        desglose.Sujeta.Exenta.Should().Contain(e => e.CausaExencion == CausaExencionType.E5 && e.BaseImponible == "30.00");

        desglose.Sujeta.NoExenta.Should().HaveCount(2);
        var s1 = desglose.Sujeta.NoExenta.Should().ContainSingle(x => x.TipoNoExenta == TipoOperacionSujetaNoExentaType.S1).Subject;
        s1.DesgloseIVA.Should().HaveCount(2);
        var s2 = desglose.Sujeta.NoExenta.Should().ContainSingle(x => x.TipoNoExenta == TipoOperacionSujetaNoExentaType.S2).Subject;
        s2.DesgloseIVA.Should().HaveCount(1);

        desglose.NoSujeta.Should().HaveCount(2);
        desglose.NoSujeta.Should().Contain(n => n.Causa == CausaNoSujetaType.OT && n.Importe == "80.00");
        desglose.NoSujeta.Should().Contain(n => n.Causa == CausaNoSujetaType.RL && n.Importe == "20.00");

        ticketBai.Factura.DatosFactura.Claves.Should().HaveCount(1);
        ticketBai.Factura.DatosFactura.Claves[0].ClaveRegimenIvaOpTrascendencia
            .Should().Be(IdOperacionesTrascendenciaTributariaType.Item01);
    }

    [Fact]
    public void ConvertTo_DomesticMixedWithExports_ProducesBothItem01AndItem02Claves()
    {
        var factory = new TicketBaiFactory(BuildConfig());

        var ticketBai = factory.ConvertTo(BuildRequest([
            ChargeItem(amount: 121m, vatRate: 21m, vatAmount: 21m, ChargeItemCaseNatureOfVatES.UsualVatApplies),
            ChargeItem(amount: 50m, vatRate: 0m, vatAmount: 0m, ChargeItemCaseNatureOfVatES.ExteptArticle21)
        ]));

        ticketBai.Factura.DatosFactura.Claves.Should().HaveCount(2);
        ticketBai.Factura.DatosFactura.Claves.Should().Contain(c =>
            c.ClaveRegimenIvaOpTrascendencia == IdOperacionesTrascendenciaTributariaType.Item01);
        ticketBai.Factura.DatosFactura.Claves.Should().Contain(c =>
            c.ClaveRegimenIvaOpTrascendencia == IdOperacionesTrascendenciaTributariaType.Item02);
    }

    [Fact]
    public void ConvertTo_ForeignCustomerWithMixedItems_RoutesToEntregaAndPrestacionServicios()
    {
        var factory = new TicketBaiFactory(BuildConfig());

        var entregaItem = ChargeItem(amount: 121m, vatRate: 21m, vatAmount: 21m, ChargeItemCaseNatureOfVatES.UsualVatApplies);
        entregaItem.ftChargeItemCase = (ChargeItemCase)((long) entregaItem.ftChargeItemCase | (long) ChargeItemCaseTypeOfService.Delivery);

        var serviceItem = ChargeItem(amount: 50m, vatRate: 0m, vatAmount: 0m, ChargeItemCaseNatureOfVatES.ExteptArticle21);
        serviceItem.ftChargeItemCase = (ChargeItemCase)((long) serviceItem.ftChargeItemCase | (long) ChargeItemCaseTypeOfService.OtherService);

        var customer = new { CustomerCountry = "FR", CustomerName = "Some FR Customer" };

        var ticketBai = factory.ConvertTo(BuildRequest([entregaItem, serviceItem], customer));

        var desglose = ticketBai.Factura.TipoDesglose.Item.Should().BeOfType<DesgloseTipoOperacionType>().Subject;
        desglose.Entrega.Should().NotBeNull();
        desglose.Entrega.Sujeta.NoExenta.Should().HaveCount(1);
        desglose.Entrega.Sujeta.NoExenta[0].TipoNoExenta.Should().Be(TipoOperacionSujetaNoExentaType.S1);

        desglose.PrestacionServicios.Should().NotBeNull();
        desglose.PrestacionServicios.Sujeta.Exenta.Should().HaveCount(1);
        desglose.PrestacionServicios.Sujeta.Exenta[0].CausaExencion.Should().Be(CausaExencionType.E2);
        desglose.PrestacionServicios.Sujeta.Exenta[0].BaseImponible.Should().Be("50.00");
    }
}
