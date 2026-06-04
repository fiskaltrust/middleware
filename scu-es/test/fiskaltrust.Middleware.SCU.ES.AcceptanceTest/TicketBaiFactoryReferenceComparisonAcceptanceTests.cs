using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.SCU.ES.Common;
using fiskaltrust.Middleware.SCU.ES.Common.Models;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common;
using fiskaltrust.Middleware.SCU.ES.TicketBAI.Common.Helpers;
using FluentAssertions;
using FluentAssertions.Execution;

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;

/// <summary>
/// Compares the factory's produced XML against the published TicketBAI samples from
/// market-es#95 on the spec-meaningful elements (branch placement, Causa/Tipo codes,
/// ClaveRegimen). Byte-exact comparison is not possible because the samples are
/// hand-authored, include placeholder signatures and use arbitrary line-item values.
/// </summary>
public class TicketBaiFactoryReferenceComparisonAcceptanceTests
{
    private static readonly XNamespace Tbai = "urn:ticketbai:emision";

    [Fact]
    public void NN10_Exports_MatchesReference_E2_Clave02_EntregaExenta()
    {
        AssertMatchesReference(
            "010_TBAI-export-transaction.xml",
            nature: ChargeItemCaseNatureOfVatES.ExteptArticle21,
            withForeignCustomer: true,
            typeOfService: ChargeItemCaseTypeOfService.Delivery);
    }

    [Fact]
    public void NN11_IntraCommunityDelivery_MatchesReference_E5_Clave01_EntregaExenta()
    {
        AssertMatchesReference(
            "007_TBAI-exempt-transaction-Art-25-intra-community-trade.xml",
            nature: ChargeItemCaseNatureOfVatES.ExteptArticle25,
            withForeignCustomer: true,
            typeOfService: ChargeItemCaseTypeOfService.Delivery);
    }

    [Fact]
    public void NN13_TreatedAsExports_MatchesReference_E3_Clave02_EntregaExenta()
    {
        AssertMatchesReference(
            "005_TBAI-exempt-transaction-Art-22.xml",
            nature: ChargeItemCaseNatureOfVatES.ExteptArticle22,
            withForeignCustomer: true,
            typeOfService: ChargeItemCaseTypeOfService.Delivery);
    }

    [Fact]
    public void NN14_CustomsExemption_MatchesReference_E4_Clave02_EntregaExenta()
    {
        AssertMatchesReference(
            "006_TBAI-exempt-transaction-Art-23-24.xml",
            nature: ChargeItemCaseNatureOfVatES.ExteptArticle23And24,
            withForeignCustomer: true,
            typeOfService: ChargeItemCaseTypeOfService.Delivery);
    }

    [Fact]
    public void NN20_NotSubjectLocationRules_MatchesReference_RL_Clave01()
    {
        AssertMatchesReference(
            "003_TBAI-not-subject-to-location-rules-transaction.xml",
            nature: ChargeItemCaseNatureOfVatES.NotSubjectLocationRules,
            withForeignCustomer: false);
    }

    [Fact]
    public void NN21_NotSubjectArt7And14_MatchesReference_OT_Clave01()
    {
        AssertMatchesReference(
            "002_TBAI-domestic-not-subject-transaction.xml",
            nature: ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14,
            withForeignCustomer: false);
    }

    [Fact]
    public void NN30_ExemptDomesticArt20_MatchesReference_E1_Clave01_DomesticExenta()
    {
        AssertMatchesReference(
            "004_TBAI-exempt-domestic-transaction-Art-20.xml",
            nature: ChargeItemCaseNatureOfVatES.ExteptArticle20,
            withForeignCustomer: false);
    }

    [Fact]
    public void NN31_OtherExemptions_MatchesReference_E6_Clave01_DomesticExenta()
    {
        AssertMatchesReference(
            "008_TBAI-exempt-transaction-others.xml",
            nature: ChargeItemCaseNatureOfVatES.ExteptOthers,
            withForeignCustomer: false);
    }

    [Fact]
    public void NN50_ReverseCharge_MatchesReferenceCodesOnly_S2_Clave01()
    {
        // The published sample puts reverse-charge inside DesgloseTipoOperacion/PrestacionServicios
        // even though there is no foreign recipient. Our factory follows the recipient-country rule
        // (domestic → DesgloseFactura). We compare only the codes here, not the branch placement.
        var reference = LoadReference("009_TBAI-reverse-charge-transaction.xml");
        var produced = ProduceTicketBaiXml(
            nature: ChargeItemCaseNatureOfVatES.ReverseCharge,
            withForeignCustomer: false,
            typeOfService: null);

        using var _ = new AssertionScope();
        ExtractClaveRegimen(produced).Should().BeEquivalentTo(ExtractClaveRegimen(reference));
        ExtractAllTipoNoExenta(produced).Should().Contain("S2");
        ExtractAllTipoNoExenta(reference).Should().Contain("S2");
    }

    [Fact(Skip = "NN [60] (foreign tax IE): ForeignTaxApplies exists in the enum but the mapper does not yet translate it to a NoSujeta/IE branch (and Clave 08 is unconfirmed); reference kept as documentation.")]
    public void NN60_ForeignTax_MatchesReference_IE_Clave08()
    {
        // Placeholder: will be enabled when TicketBaiNatureOfVatMapping maps ForeignTaxApplies
        // to NoSujeta/Causa=IE with the correct ClaveRegimen (08, pending confirmation).
    }

    private static void AssertMatchesReference(
        string referenceFileName,
        ChargeItemCaseNatureOfVatES nature,
        bool withForeignCustomer,
        ChargeItemCaseTypeOfService? typeOfService = null)
    {
        var reference = LoadReference(referenceFileName);
        var produced = ProduceTicketBaiXml(nature, withForeignCustomer, typeOfService);

        using var _ = new AssertionScope($"reference '{referenceFileName}'");

        // L9 — ClaveRegimenIvaOpTrascendencia values must match (order doesn't matter).
        ExtractClaveRegimen(produced).Should().BeEquivalentTo(ExtractClaveRegimen(reference));

        // L10 / L11 / L13 — exempt / non-exempt / not-subject codes must match.
        ExtractAllCausaExencion(produced).Should().BeEquivalentTo(ExtractAllCausaExencion(reference));
        ExtractAllTipoNoExenta(produced).Should().BeEquivalentTo(ExtractAllTipoNoExenta(reference));
        ExtractAllCausaNoSujeta(produced).Should().BeEquivalentTo(ExtractAllCausaNoSujeta(reference));

        // Branch placement — DesgloseFactura vs DesgloseTipoOperacion (Entrega vs PrestacionServicios).
        ExtractDesgloseShape(produced).Should().Be(ExtractDesgloseShape(reference));
    }

    private static XDocument LoadReference(string fileName)
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var projectDir = new DirectoryInfo(dir);
        while (projectDir != null && !File.Exists(Path.Combine(projectDir.FullName, "fiskaltrust.Middleware.SCU.ES.AcceptanceTest.csproj")))
        {
            projectDir = projectDir.Parent;
        }
        if (projectDir == null)
        {
            throw new InvalidOperationException("Could not locate the AcceptanceTest project directory from " + dir);
        }
        var path = Path.Combine(projectDir.FullName, "TestData", "TicketBAI", "ReferenceSamples", fileName);
        return XDocument.Load(path);
    }

    private static XDocument ProduceTicketBaiXml(
        ChargeItemCaseNatureOfVatES nature,
        bool withForeignCustomer,
        ChargeItemCaseTypeOfService? typeOfService)
    {
        var caseValue = (long) nature;
        if (typeOfService is { } tos)
        {
            caseValue |= (long) tos;
        }

        var chargeItem = new ChargeItem
        {
            Quantity = 1m,
            Amount = 121m,
            VATRate = 21m,
            VATAmount = 21m,
            Description = "test",
            ftChargeItemCase = (ChargeItemCase) caseValue
        };

        var customer = withForeignCustomer
            ? new MiddlewareCustomer { CustomerCountry = "FR", CustomerName = "Test FR Customer" }
            : null;

        var request = new ReceiptRequest
        {
            cbReceiptReference = "ACC-REF-001",
            cbReceiptMoment = new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc),
            cbChargeItems = [chargeItem],
            cbCustomer = customer,
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001.WithCountry("ES").WithVersion(2)
        };
        var processRequest = JsonSerializer.Deserialize<ProcessRequest>(JsonSerializer.Serialize(new ProcessRequest
        {
            ReceiptRequest = request,
            ReceiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "0#TBAI/1",
                ftReceiptMoment = request.cbReceiptMoment,
                ftCashBoxIdentification = "ACC-CASHBOX-01",
                ftStateData = new MiddlewareStateData { ES = new MiddlewareStateDataES() }
            }
        }))!;

        var factory = new TicketBaiFactory(new TicketBaiSCUConfiguration
        {
            EmisorNif = "B10646545",
            EmisorApellidosNombreRazonSocial = "ACCEPTANCE TEST EMISOR S.L.",
            SoftwareVersion = "1.0.0",
            SoftwareName = "fiskaltrust.Middleware",
            SoftwareLicenciaTBAI = "TBAITEST0000000000001",
            SoftwareNif = "B10646545"
        });
        var ticketBai = factory.ConvertTo(processRequest);
        return XDocument.Parse(XmlHelpers.GetXMLIncludingNamespace(ticketBai));
    }

    private static IEnumerable<string> ExtractClaveRegimen(XDocument doc) =>
        doc.Descendants(Tbai + "ClaveRegimenIvaOpTrascendencia")
           .Concat(doc.Descendants("ClaveRegimenIvaOpTrascendencia"))
           .Select(e => e.Value.Trim());

    private static IEnumerable<string> ExtractAllCausaExencion(XDocument doc) =>
        doc.Descendants(Tbai + "CausaExencion")
           .Concat(doc.Descendants("CausaExencion"))
           .Select(e => e.Value.Trim());

    private static IEnumerable<string> ExtractAllTipoNoExenta(XDocument doc) =>
        doc.Descendants(Tbai + "TipoNoExenta")
           .Concat(doc.Descendants("TipoNoExenta"))
           .Select(e => e.Value.Trim());

    private static IEnumerable<string> ExtractAllCausaNoSujeta(XDocument doc) =>
        doc.Descendants(Tbai + "Causa")
           .Concat(doc.Descendants("Causa"))
           .Where(e => e.Parent?.Name.LocalName == "DetalleNoSujeta")
           .Select(e => e.Value.Trim());

    /// <summary>
    /// Returns a short string describing where the breakdown lives:
    /// e.g. "DesgloseFactura+Sujeta/Exenta", "DesgloseTipoOperacion/Entrega+Sujeta/Exenta",
    /// "DesgloseFactura+NoSujeta". Robust to whether the reference XML wraps the breakdown
    /// in &lt;TipoDesglose&gt; (the samples sometimes omit it).
    /// </summary>
    private static string ExtractDesgloseShape(XDocument doc)
    {
        var desgloseFactura = FindAny(doc, "DesgloseFactura");
        var desgloseTipoOperacion = FindAny(doc, "DesgloseTipoOperacion");

        var parts = new List<string>();
        if (desgloseFactura != null)
        {
            parts.Add("DesgloseFactura" + DescribeSujetaAndNoSujeta(desgloseFactura));
        }
        if (desgloseTipoOperacion != null)
        {
            var entrega = desgloseTipoOperacion.Elements().FirstOrDefault(e => e.Name.LocalName == "Entrega");
            var prestacion = desgloseTipoOperacion.Elements().FirstOrDefault(e => e.Name.LocalName == "PrestacionServicios");
            if (entrega != null)
            {
                parts.Add("DesgloseTipoOperacion/Entrega" + DescribeSujetaAndNoSujeta(entrega));
            }
            if (prestacion != null)
            {
                parts.Add("DesgloseTipoOperacion/PrestacionServicios" + DescribeSujetaAndNoSujeta(prestacion));
            }
        }
        return string.Join("|", parts.OrderBy(x => x));
    }

    private static string DescribeSujetaAndNoSujeta(XElement branch)
    {
        var parts = new List<string>();
        var sujeta = branch.Elements().FirstOrDefault(e => e.Name.LocalName == "Sujeta");
        if (sujeta != null)
        {
            if (sujeta.Elements().Any(e => e.Name.LocalName == "Exenta"))
            {
                parts.Add("Sujeta/Exenta");
            }
            if (sujeta.Elements().Any(e => e.Name.LocalName == "NoExenta"))
            {
                parts.Add("Sujeta/NoExenta");
            }
        }
        if (branch.Elements().Any(e => e.Name.LocalName == "NoSujeta"))
        {
            parts.Add("NoSujeta");
        }
        return parts.Count == 0 ? string.Empty : "+" + string.Join(",", parts);
    }

    private static XElement? FindAny(XDocument doc, string localName) =>
        doc.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);
}
