using System;
using System.IO;
using System.Xml.Linq;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.ES.VeriFactu;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Mapping;
using fiskaltrust.Middleware.SCU.ES.VeriFactu.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.ES.AcceptanceTest;

// Golden-file tests: compare the XML produced by VeriFactuMapping against committed
// payloads under TestData/VeriFactu. Set UPDATE_GOLDEN=1 to regenerate the files when
// the mapper's output legitimately changes; review the diff before committing.
public class VeriFactuMappingXmlAcceptanceTests
{
    // Pinned values so Huella (SHA-256 over fields) and FechaHoraHusoGenRegistro stay stable.
    private static readonly DateTime FixedReceiptMoment = new(2026, 5, 22, 8, 30, 15, DateTimeKind.Utc);
    private const string FixedReceiptReference = "POS-acceptance-0001";
    private const string FixedCashBoxIdentification = "AAAAAAAAAA";
    private const string FixedNif = "M0291081Q";
    private const string FixedEmisor = "Acceptance Test Emisor";

    private const ulong EsServiceFlag = 0x4752_2000_0000_0000;

    public static TheoryData<string, ChargeItemCaseNatureOfVatES, VeriFactuTaxRegime> Scenarios()
        => new()
        {
            // file name slug, NatureOfVat, regime
            { "usual-vat-mainland",       ChargeItemCaseNatureOfVatES.UsualVatApplies,        VeriFactuTaxRegime.MainlandVat },
            { "export-article21-mainland", ChargeItemCaseNatureOfVatES.Exports,              VeriFactuTaxRegime.MainlandVat },
            { "exempt-article20-mainland", ChargeItemCaseNatureOfVatES.ExemptedDomestic,     VeriFactuTaxRegime.MainlandVat },
            { "reverse-charge-mainland",   ChargeItemCaseNatureOfVatES.ReverseCharge,         VeriFactuTaxRegime.MainlandVat },
            { "non-subject-art7and14",     ChargeItemCaseNatureOfVatES.NotSubjectArticle7and14, VeriFactuTaxRegime.MainlandVat },
            { "foreign-tax-applies-NN60",  ChargeItemCaseNatureOfVatES.ForeignTaxApplies,     VeriFactuTaxRegime.MainlandVat },
            { "excluded-third-party-NN80", ChargeItemCaseNatureOfVatES.ExcludedThirdParty,    VeriFactuTaxRegime.MainlandVat },
            { "usual-vat-igic",            ChargeItemCaseNatureOfVatES.UsualVatApplies,       VeriFactuTaxRegime.IGIC },
            { "usual-vat-ipsi",            ChargeItemCaseNatureOfVatES.UsualVatApplies,       VeriFactuTaxRegime.IPSI },
        };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void CreateRegistroFacturacionAlta_XmlMatchesGoldenPayload(
        string slug, ChargeItemCaseNatureOfVatES nature, VeriFactuTaxRegime regime)
    {
        var config = new VeriFactuSCUConfiguration
        {
            Nif = FixedNif,
            NombreRazonEmisor = FixedEmisor,
            TaxRegime = regime,
        };
        var mapping = new VeriFactuMapping(config, signXml: false);
        var (request, response) = BuildDeterministicRequestResponse(nature);

        var alta = mapping.CreateRegistroFacturacionAlta(request, response, null, null);
        var actualXml = SerializeIndented(alta);

        AssertOrUpdateGolden($"{slug}.xml", actualXml);
    }

    private static (ReceiptRequest, ReceiptResponse) BuildDeterministicRequestResponse(ChargeItemCaseNatureOfVatES nature)
    {
        var isExempt = nature != ChargeItemCaseNatureOfVatES.UsualVatApplies;
        var vatRate = isExempt ? 0m : 21m;
        var amount = isExempt ? 10m : 12.10m;
        var vatAmount = isExempt ? 0m : 2.10m;

        var chargeItemCase = (ChargeItemCase) (EsServiceFlag | (ulong) nature);

        var request = new ReceiptRequest
        {
            ftCashBoxID = new Guid("00000000-0000-0000-0000-000000000001"),
            ftReceiptCase = (ReceiptCase) EsServiceFlag,
            cbTerminalID = "1",
            cbReceiptReference = FixedReceiptReference,
            cbReceiptMoment = FixedReceiptMoment,
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
                    Description = "Acceptance test item"
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
            ftQueueID = new Guid("00000000-0000-0000-0000-000000000002"),
            ftQueueItemID = new Guid("00000000-0000-0000-0000-000000000003"),
            ftQueueRow = 0,
            ftCashBoxIdentification = FixedCashBoxIdentification,
            ftReceiptIdentification = $"0#0/{FixedReceiptReference}",
            ftReceiptMoment = FixedReceiptMoment,
            ftState = (State) EsServiceFlag,
        };

        return (request, response);
    }

    private static string SerializeIndented(RegistroFacturacionAlta alta)
    {
        var xml = alta.XmlSerialize();
        // Round-trip through XDocument so the file diff stays stable regardless of attribute order tweaks
        // in different XmlSerializer versions, and so the golden files are human-readable.
        var doc = XDocument.Parse(xml);
        return doc.ToString(SaveOptions.None);
    }

    private static void AssertOrUpdateGolden(string fileName, string actualXml)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "VeriFactu", fileName);
        var update = Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1";

        if (update || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actualXml);

            // Also write back to the source tree so the file gets picked up by the next build / commit.
            // AppContext.BaseDirectory is .../bin/Debug/net8.0; the source TestData is two levels up.
            var sourcePath = ResolveSourcePath(fileName);
            if (sourcePath is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
                File.WriteAllText(sourcePath, actualXml);
            }
            return;
        }

        var expectedXml = File.ReadAllText(path);
        Normalize(actualXml).Should().Be(Normalize(expectedXml),
            $"Generated VeriFactu XML for '{fileName}' must match the committed golden payload. " +
            $"If the mapper's output legitimately changed, regenerate with UPDATE_GOLDEN=1 and review the diff.");
    }

    private static string Normalize(string xml) => XDocument.Parse(xml).ToString(SaveOptions.None);

    private static string? ResolveSourcePath(string fileName)
    {
        // Walk up from bin/Debug/net8.0 to the project root (where the .csproj lives).
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var projectFile = Path.Combine(dir.FullName, "fiskaltrust.Middleware.SCU.ES.AcceptanceTest.csproj");
            if (File.Exists(projectFile))
            {
                return Path.Combine(dir.FullName, "TestData", "VeriFactu", fileName);
            }
            dir = dir.Parent;
        }
        return null;
    }
}
