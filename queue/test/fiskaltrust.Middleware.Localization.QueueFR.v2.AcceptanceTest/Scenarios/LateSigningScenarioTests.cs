using System.Security.Cryptography;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.Middleware.SCU.FR.LNE;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>
/// Late signing end to end (market-fr#231), against a real certification-body SCU: the POS re-sends
/// the receipts it issued while the middleware was unreachable, the queue answers in late-signing
/// mode until a zero receipt closes it, and the POS sends the JET events the queue asks for.
/// </summary>
public abstract class LateSigningScenarioTests
{
    private const string Siret = "12345678901234";

    private readonly Func<string, Task<string>> _signProcessor;
    private readonly Guid _cashBoxId = Guid.NewGuid();

    protected abstract IFRSSCD CreateScu();

    protected static string NewPrivateKey()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return Convert.ToBase64String(key.ExportPkcs8PrivateKey());
    }

    protected LateSigningScenarioTests()
    {
        var queueId = Guid.NewGuid();
        var configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox { ftCashBoxId = _cashBoxId, TimeStamp = DateTime.UtcNow.Ticks }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue> { new() { ftQueueId = queueId, ftCashBoxId = _cashBoxId, StartMoment = DateTime.UtcNow, CountryCode = "FR" } }) },
            { "init_ftQueueFR", JsonSerializer.Serialize(new List<ftQueueFR> { new() { ftQueueFRId = queueId, CashBoxIdentification = "cashbox-fr", Siret = Siret } }) },
            { "init_ftSignaturCreationUnitFR", JsonSerializer.Serialize(new List<ftSignaturCreationUnitFR>()) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration
                {
                    Account = new AccountMasterData { AccountId = Guid.NewGuid(), AccountName = "fiskaltrust SARL", Street = "1 rue de la Paix", Zip = "75002", City = "Paris", Country = "FR" },
                })
            },
        };

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var storageProvider = new InMemoryLocalizationStorageProvider(queueId, configuration, loggerFactory);
        _signProcessor = new QueueFRBootstrapper(queueId, loggerFactory, configuration, CreateScu(), storageProvider).RegisterForSign();
    }

    [Fact]
    public async Task LateSignedReceipts_AreSignedInOrder_AndTheModeRunsUntilTheZeroReceipt()
    {
        var before = await SignAsync(CashSale());
        var offline = DateTime.UtcNow.AddHours(-3);

        var firstLate = await SignAsync(CashSale(lateSigned: true, cbReceiptMoment: offline));
        var secondLate = await SignAsync(CashSale(lateSigned: true, cbReceiptMoment: offline.AddMinutes(10)));
        var ordinary = await SignAsync(CashSale());
        var zero = await SignAsync(Request(ReceiptCase.ZeroReceipt0x2000));
        var after = await SignAsync(CashSale());

        foreach (var response in new[] { before, firstLate, secondLate, ordinary, zero, after })
        {
            response.ftState.IsState(State.Error).Should().BeFalse(string.Join(" / ", response.ftSignatures.Select(x => x.Data)));
            response.ftSignatures.Should().Contain(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature), "a late-signed receipt is signed like any other");
        }

        new[] { before, firstLate, secondLate, ordinary, after }.Select(x => x.ftReceiptIdentification[^2..]).Should().Equal("T1", "T2", "T3", "T4", "T5");

        InMode(before).Should().BeFalse();
        InMode(firstLate).Should().BeTrue();
        InMode(secondLate).Should().BeTrue();
        InMode(ordinary).Should().BeTrue();
        InMode(zero).Should().BeFalse();
        InMode(after).Should().BeFalse();

        JetCodes(firstLate).Should().Equal("70");
        JetCodes(secondLate).Should().BeEmpty();
        JetCodes(ordinary).Should().BeEmpty();
        JetCodes(zero).Should().BeEmpty("the POS sends JET 120 on its own");
        JetCodes(after).Should().BeEmpty();

        zero.ftSignatures.Single(x => x.Caption == "Défaillance enregistrée").Data
            .Should().Be($"du {firstLate.ftReceiptIdentification} au {zero.ftReceiptIdentification}");
    }

    [Fact]
    public async Task ThePosSendsTheRequestedJetEvents_AndTheyAreSignedOnTheLogChain()
    {
        var late = await SignAsync(CashSale(lateSigned: true, cbReceiptMoment: DateTime.UtcNow.AddHours(-1)));
        var zero = await SignAsync(Request(ReceiptCase.ZeroReceipt0x2000));

        // JET 70 is prompted by the item on the first late-signed receipt; JET 120 is the POS's own
        // event when it leaves the mode, sent without a prompt (market-fr#200, 2026-11-05).
        zero.ftSignatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent));
        var posOwn120 = "{\"FR\":{\"Code\":\"120\",\"Message\":\"Fin de mode dégradé\",\"Information\":\"reconnected\"}}";
        foreach (var (trigger, code, caseData) in new[] { (late, "70", late.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent)).Data), (zero, "120", posOwn120) })
        {
            var jetEvent = Request(ReceiptCase.ProtocolTechnicalEvent0x3001);
            jetEvent.ftReceiptCaseData = caseData;
            jetEvent.cbPreviousReceiptReference = RequestReferenceOf(trigger);

            var response = await SignAsync(jetEvent);

            response.ftState.IsState(State.Error).Should().BeFalse($"JET {code}: {string.Join(" / ", response.ftSignatures.Select(x => x.Data))}");
            response.ftReceiptIdentification.Should().MatchRegex("L[0-9]+$");
        }
    }

    private readonly Dictionary<Guid, string> _references = new();

    private string RequestReferenceOf(ReceiptResponse response) => _references[response.ftQueueItemID];

    private async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
    {
        var response = JsonSerializer.Deserialize<ReceiptResponse>(await _signProcessor(JsonSerializer.Serialize(request)))!;
        _references[response.ftQueueItemID] = request.cbReceiptReference!;
        return response;
    }

    private static bool InMode(ReceiptResponse response) => response.ftState.IsFlag(StateFlags.LateSigningModeIsActive);

    private static IEnumerable<string?> JetCodes(ReceiptResponse response)
        => response.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent))
            .Select(x => JsonDocument.Parse(x.Data).RootElement.GetProperty("FR").GetProperty("Code").GetString());

    private ReceiptRequest Request(ReceiptCase receiptCase, bool lateSigned = false, DateTime? cbReceiptMoment = null) => new()
    {
        ftCashBoxID = _cashBoxId,
        cbTerminalID = "1",
        cbUser = "operator-1",
        cbReceiptReference = Guid.NewGuid().ToString("N")[..8],
        cbReceiptMoment = cbReceiptMoment ?? DateTime.UtcNow,
        Currency = Currency.EUR,
        ftReceiptCase = lateSigned ? receiptCase.WithCountry("FR").WithFlag(ReceiptCaseFlags.LateSigning) : receiptCase.WithCountry("FR"),
        cbChargeItems = [],
        cbPayItems = [],
    };

    private ReceiptRequest CashSale(bool lateSigned = false, DateTime? cbReceiptMoment = null)
    {
        var request = Request(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned, cbReceiptMoment);
        request.cbChargeItems = [new ChargeItem { Amount = 12.00m, VATAmount = 2.00m, VATRate = 20m, Quantity = 1, Description = "Cafe", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }];
        request.cbPayItems = [new PayItem { Amount = 12.00m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }];
        return request;
    }
}

public class LneLateSigningScenarioTests : LateSigningScenarioTests
{
    protected override IFRSSCD CreateScu() => new LneFRSSCD(new LneConfiguration
    {
        Siret = "12345678901234",
        PrivateKey = NewPrivateKey(),
        CertificateSerialNumber = "LNE-0001",
        LneCertificateNumber = "LNE-2026-042",
    });
}

public class InfoCertLateSigningScenarioTests : LateSigningScenarioTests
{
    protected override IFRSSCD CreateScu() => new InfoCertFRSSCD(new InfoCertConfiguration
    {
        Siret = "12345678901234",
        PrivateKey = NewPrivateKey(),
        CertificateSerialNumber = "CERT-4711",
        AttestationNumber = "IC-2026-001",
        SoftwareVersion = "1.0.0",
    });
}
