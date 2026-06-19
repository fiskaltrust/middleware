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

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;

public class TicketBaiFactoryXmlAcceptanceTests
{
    // Set this env var to "1" to overwrite the .xml snapshots with the freshly generated output.
    // Intended for the first run / when the spec changes — diffs should be reviewed before commit.
    private const string UpdateEnvVar = "UPDATE_TICKETBAI_SNAPSHOTS";

    private static readonly DateTime FixedReceiptMoment = new(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Domestic_UsualVat_SingleItem()
    {
        var ticketBai = Build([
            Item(amount: 121m, vatRate: 21m, vatAmount: 21m, description: "Product A",
                ChargeItemCaseNatureOfVatES.UsualVatApplies)
        ]);

        AssertSnapshot(ticketBai, "domestic-usual-vat-single.xml");
    }

    [Fact]
    public void Domestic_ReverseCharge_SingleItem()
    {
        var ticketBai = Build([
            Item(amount: 100m, vatRate: 0m, vatAmount: 0m, description: "Reverse-charge service",
                ChargeItemCaseNatureOfVatES.ReverseCharge)
        ]);

        AssertSnapshot(ticketBai, "domestic-reverse-charge.xml");
    }

    [Fact]
    public void Domestic_ExportExempt_SingleItem()
    {
        var ticketBai = Build([
            Item(amount: 50m, vatRate: 0m, vatAmount: 0m, description: "Exported good",
                ChargeItemCaseNatureOfVatES.Exports)
        ]);

        AssertSnapshot(ticketBai, "domestic-exempt-exports.xml");
    }

    [Fact]
    public void Domestic_IntraCommunityDelivery_SingleItem()
    {
        var ticketBai = Build([
            Item(amount: 200m, vatRate: 0m, vatAmount: 0m, description: "Intra-EU delivery",
                ChargeItemCaseNatureOfVatES.IntraCommunityDelivery)
        ]);

        AssertSnapshot(ticketBai, "domestic-exempt-intracommunity.xml");
    }

    [Fact]
    public void Domestic_NotSubject_LocationRules()
    {
        var ticketBai = Build([
            Item(amount: 80m, vatRate: 0m, vatAmount: 0m, description: "Out-of-scope service",
                ChargeItemCaseNatureOfVatES.NotSubjectLocationRules)
        ]);

        AssertSnapshot(ticketBai, "domestic-not-subject-location.xml");
    }

    [Fact]
    public void Domestic_NotSubject_Article7and14()
    {
        var ticketBai = Build([
            Item(amount: 30m, vatRate: 0m, vatAmount: 0m, description: "Not-subject art. 7/14",
                ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14)
        ]);

        AssertSnapshot(ticketBai, "domestic-not-subject-art7and14.xml");
    }

    [Fact]
    public void Domestic_MixedAllBranches()
    {
        var ticketBai = Build([
            Item(amount: 121m, vatRate: 21m, vatAmount: 21m, description: "Product 21%",
                ChargeItemCaseNatureOfVatES.UsualVatApplies),
            Item(amount: 110m, vatRate: 10m, vatAmount: 10m, description: "Product 10%",
                ChargeItemCaseNatureOfVatES.UsualVatApplies),
            Item(amount: 100m, vatRate: 0m, vatAmount: 0m, description: "Reverse-charge service",
                ChargeItemCaseNatureOfVatES.ReverseCharge),
            Item(amount: 50m, vatRate: 0m, vatAmount: 0m, description: "Exempt domestic",
                ChargeItemCaseNatureOfVatES.ExemptedDomestic),
            Item(amount: 30m, vatRate: 0m, vatAmount: 0m, description: "Intra-EU delivery",
                ChargeItemCaseNatureOfVatES.IntraCommunityDelivery),
            Item(amount: 80m, vatRate: 0m, vatAmount: 0m, description: "Not-subject art. 7/14",
                ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14),
            Item(amount: 20m, vatRate: 0m, vatAmount: 0m, description: "Out-of-scope service",
                ChargeItemCaseNatureOfVatES.NotSubjectLocationRules)
        ]);

        AssertSnapshot(ticketBai, "domestic-mixed-all-branches.xml");
    }

    [Fact]
    public void Domestic_MixedWithExports_ProducesBothClaves()
    {
        var ticketBai = Build([
            Item(amount: 121m, vatRate: 21m, vatAmount: 21m, description: "Product 21%",
                ChargeItemCaseNatureOfVatES.UsualVatApplies),
            Item(amount: 50m, vatRate: 0m, vatAmount: 0m, description: "Exported good",
                ChargeItemCaseNatureOfVatES.Exports)
        ]);

        AssertSnapshot(ticketBai, "domestic-mixed-with-exports.xml");
    }

    [Fact]
    public void Foreign_ExportGoodsAndDomesticService_SplitsAcrossEntregaAndPrestacion()
    {
        // Pick TypeOfService values that fall in only one of the factory's two filter lists,
        // so each item lands in exactly one branch (Delivery → Entrega, Tip → PrestacionServicios).
        var goods = Item(amount: 50m, vatRate: 0m, vatAmount: 0m, description: "Exported good",
            ChargeItemCaseNatureOfVatES.Exports);
        goods.ftChargeItemCase = (ChargeItemCase)((long) goods.ftChargeItemCase | (long) ChargeItemCaseTypeOfService.Delivery);

        var service = Item(amount: 110m, vatRate: 10m, vatAmount: 10m, description: "Standard service",
            ChargeItemCaseNatureOfVatES.UsualVatApplies);
        service.ftChargeItemCase = (ChargeItemCase)((long) service.ftChargeItemCase | (long) ChargeItemCaseTypeOfService.Tip);

        var customer = new MiddlewareCustomer
        {
            CustomerCountry = "FR",
            CustomerName = "French Customer",
            CustomerStreet = "1 Rue Example",
            CustomerZip = "75001"
        };

        var ticketBai = Build([goods, service], customer);

        AssertSnapshot(ticketBai, "foreign-export-goods-and-service.xml");
    }

    [Fact]
    public void Domestic_UsualVat_MultipleRates_AllStandard()
    {
        // Regression lock for the pre-exempt-reasons behaviour: a plain multi-rate domestic
        // invoice (every line UsualVatApplies) must still produce a single Sujeta/NoExenta/S1
        // branch holding one DetalleIVA per VAT rate, with Claves=01. This is the most common
        // standard invoice shape and was previously only covered indirectly inside the mixed test.
        var ticketBai = Build([
            Item(amount: 121m, vatRate: 21m, vatAmount: 21m, description: "Standard 21%",
                ChargeItemCaseNatureOfVatES.UsualVatApplies),
            Item(amount: 110m, vatRate: 10m, vatAmount: 10m, description: "Reduced 10%",
                ChargeItemCaseNatureOfVatES.UsualVatApplies),
            Item(amount: 104m, vatRate: 4m, vatAmount: 4m, description: "Super-reduced 4%",
                ChargeItemCaseNatureOfVatES.UsualVatApplies)
        ]);

        AssertSnapshot(ticketBai, "domestic-usual-vat-multirate.xml");
    }

    [Fact]
    public void Foreign_UsualVat_GoodsAndService_AllStandard()
    {
        // Regression lock for the pre-exempt-reasons behaviour: a foreign-recipient invoice with
        // ordinary taxable goods and services must still produce DesgloseTipoOperacion with
        // Entrega/Sujeta/NoExenta/S1 and PrestacionServicios/Sujeta/NoExenta/S1, Claves=01.
        // Delivery and Tip each fall in only one of the factory's two TypeOfService lists,
        // so each line lands in exactly one branch.
        var goods = Item(amount: 121m, vatRate: 21m, vatAmount: 21m, description: "Standard goods",
            ChargeItemCaseNatureOfVatES.UsualVatApplies);
        goods.ftChargeItemCase = (ChargeItemCase)((long) goods.ftChargeItemCase | (long) ChargeItemCaseTypeOfService.Delivery);

        var service = Item(amount: 110m, vatRate: 10m, vatAmount: 10m, description: "Standard service",
            ChargeItemCaseNatureOfVatES.UsualVatApplies);
        service.ftChargeItemCase = (ChargeItemCase)((long) service.ftChargeItemCase | (long) ChargeItemCaseTypeOfService.Tip);

        var customer = new MiddlewareCustomer
        {
            CustomerCountry = "FR",
            CustomerName = "French Customer",
            CustomerVATId = "FR12345678901",
            CustomerStreet = "1 Rue Example",
            CustomerZip = "75001"
        };

        var ticketBai = Build([goods, service], customer);

        AssertSnapshot(ticketBai, "foreign-usual-vat-goods-and-service.xml");
    }

    private static ChargeItem Item(decimal amount, decimal vatRate, decimal vatAmount, string description,
        ChargeItemCaseNatureOfVatES nature) => new()
        {
            Quantity = 1m,
            Amount = amount,
            VATRate = vatRate,
            VATAmount = vatAmount,
            Description = description,
            ftChargeItemCase = (ChargeItemCase)(long) nature
        };

    private static TicketBai Build(IEnumerable<ChargeItem> items, MiddlewareCustomer? customer = null)
    {
        var factory = new TicketBaiFactory(BuildConfig());
        return factory.ConvertTo(BuildProcessRequest(items, customer));
    }

    private static TicketBaiSCUConfiguration BuildConfig() => new()
    {
        EmisorNif = "B10646545",
        EmisorApellidosNombreRazonSocial = "ACCEPTANCE TEST EMISOR S.L.",
        SoftwareVersion = "1.0.0",
        SoftwareName = "fiskaltrust.Middleware",
        SoftwareLicenciaTBAI = "TBAITEST0000000000001",
        SoftwareNif = "B10646545"
    };

    private static ProcessRequest BuildProcessRequest(IEnumerable<ChargeItem> chargeItems, MiddlewareCustomer? customer)
    {
        var request = new ReceiptRequest
        {
            cbReceiptReference = "ACC-0001",
            cbReceiptMoment = FixedReceiptMoment,
            cbChargeItems = chargeItems.ToList(),
            cbCustomer = customer,
            ftReceiptCase = ReceiptCase.InvoiceB2C0x1001.WithCountry("ES").WithVersion(2)
        };
        return JsonSerializer.Deserialize<ProcessRequest>(JsonSerializer.Serialize(new ProcessRequest
        {
            ReceiptRequest = request,
            ReceiptResponse = new ReceiptResponse
            {
                ftReceiptIdentification = "0#TBAI/1",
                ftReceiptMoment = FixedReceiptMoment,
                ftCashBoxIdentification = "ACC-CASHBOX-01",
                ftStateData = new MiddlewareStateData { ES = new MiddlewareStateDataES() }
            }
        }))!;
    }

    private static void AssertSnapshot(TicketBai ticketBai, string fileName)
    {
        var actualXml = NormalizeXml(XmlHelpers.GetXMLIncludingNamespace(ticketBai));
        var snapshotPath = GetSnapshotPath(fileName);

        if (!File.Exists(snapshotPath) || Environment.GetEnvironmentVariable(UpdateEnvVar) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            File.WriteAllText(snapshotPath, actualXml);
            throw new Xunit.Sdk.XunitException(
                $"Snapshot written to '{snapshotPath}'. Inspect the file and re-run the test.");
        }

        var expectedXml = NormalizeXml(File.ReadAllText(snapshotPath));
        actualXml.Should().Be(expectedXml,
            $"the produced XML must match the reference snapshot. " +
            $"To regenerate after a deliberate change set env var {UpdateEnvVar}=1.");
    }

    private static string NormalizeXml(string xml)
    {
        // Parse and reserialize with stable settings so insignificant whitespace / line endings
        // don't cause spurious mismatches between the file on disk and the freshly produced XML.
        var doc = XDocument.Parse(xml);
        return doc.ToString(SaveOptions.None).ReplaceLineEndings("\n");
    }

    private static string GetSnapshotPath(string fileName)
    {
        // Walk up from the test assembly's binary location to the project root, then to TestData.
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var projectDir = new DirectoryInfo(assemblyDir);
        while (projectDir != null && !File.Exists(Path.Combine(projectDir.FullName, "fiskaltrust.Middleware.SCU.ES.AcceptanceTest.csproj")))
        {
            projectDir = projectDir.Parent;
        }
        if (projectDir == null)
        {
            throw new InvalidOperationException("Could not locate the AcceptanceTest project directory from " + assemblyDir);
        }
        return Path.Combine(projectDir.FullName, "TestData", "TicketBAI", "Snapshots", fileName);
    }
}
