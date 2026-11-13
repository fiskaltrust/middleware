using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.SCU.FR.InMemory;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>
/// A v1.2 queue migrated to v2 in the cloud cashbox (route A, market-fr ADR 0003, 0017): the same
/// queue, its v1 state in ftQueueFR, none of its v1 receipts in the v2 storage.
/// </summary>
public class MigrationScenarioTests : IDisposable
{
    private readonly Guid _queueId = Guid.NewGuid();
    private readonly Guid _cashBoxId = Guid.NewGuid();
    private readonly InMemorySCU _scu = new();
    private readonly Func<string, Task<string>> _sign;
    private readonly Func<string, Task<(System.Net.Mime.ContentType contentType, PipeReader reader)>> _journal;

    public MigrationScenarioTests()
    {
        var lastDailyClosing = DateTime.UtcNow.AddHours(-2);
        var v1QueueFR = new ftQueueFR
        {
            ftQueueFRId = _queueId,
            CashBoxIdentification = "cashbox-fr",
            Siret = "12345678901234",
            TNumerator = 120,
            TLastHash = "v1-ticket-hash",
            TTotalizer = 1000m,
            GNumerator = 30,
            GLastHash = "v1-grand-total-hash",
            GLastDayMoment = lastDailyClosing,
            ANumerator = 3,
            ALastHash = "v1-archive-hash",
            ALastQueueItemId = Guid.NewGuid(),
            ALastMoment = lastDailyClosing.AddMinutes(5),
        };
        var configuration = new Dictionary<string, object>
        {
            { "FRLocalDevelopment", true },
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox { ftCashBoxId = _cashBoxId, TimeStamp = DateTime.UtcNow.Ticks }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue> { new() { ftQueueId = _queueId, ftCashBoxId = _cashBoxId, StartMoment = DateTime.UtcNow.AddYears(-2), CountryCode = "FR" } }) },
            { "init_ftQueueFR", JsonSerializer.Serialize(new List<ftQueueFR> { v1QueueFR }) },
            { "init_ftSignaturCreationUnitFR", JsonSerializer.Serialize(new List<ftSignaturCreationUnitFR>()) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration { Account = new AccountMasterData { AccountId = Guid.NewGuid(), AccountName = "fiskaltrust SARL", Country = "FR" } }) },
        };

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var bootstrapper = new QueueFRBootstrapper(_queueId, loggerFactory, configuration, _scu, new InMemoryLocalizationStorageProvider(_queueId, configuration, loggerFactory));
        _sign = bootstrapper.RegisterForSign();
        _journal = bootstrapper.RegisterForJournal();
    }

    public void Dispose() => _scu.Dispose();

    [Fact]
    public async Task AV12Queue_IsMigratedByItsTwoReceipts_AndItsChainsContinueFromTheV1Counters()
    {
        (await SignAsync(Sale())).ftState.IsState(State.Error).Should().BeTrue("a v1.2 queue accepts only the migration receipts before it is migrated");

        var initiate = await SignAsync(Request(0x4021));
        initiate.ftState.IsState(State.Error).Should().BeFalse();
        initiate.ftReceiptIdentification.Should().EndWith("E1");

        var finish = await SignAsync(Request(0x4022));
        finish.ftState.IsState(State.Error).Should().BeFalse();
        finish.ftReceiptIdentification.Should().EndWith("E2");

        var sale = await SignAsync(Sale());
        sale.ftState.IsState(State.Error).Should().BeFalse();
        sale.ftReceiptIdentification.Should().EndWith("T121", "the v1 counter continues");

        (await SignAsync(Request(0x4022))).ftState.IsState(State.Error).Should().BeTrue("the queue is migrated");
    }

    /// <summary>
    /// With no v1 receipt in the v2 storage, each v2 chain's first entry starts the chain: the signed
    /// migration event is the link to v1, so the archive marks the entry as a start of chain.
    /// </summary>
    [Fact]
    public async Task AfterTheMigration_TheFirstV2EntryOfAChain_IsArchivedAsAStartOfChain()
    {
        await SignAsync(Request(0x4021));
        await SignAsync(Request(0x4022));
        var sale = await SignAsync(Sale());

        var rows = Rows(await ReadAsync(JournalTypeFR.Ticket));

        var row = rows.Should().ContainSingle().Subject;
        row["ftReceiptIdentification"].Should().Be(sale.ftReceiptIdentification);
        row["PreviousSignature"].Should().BeEmpty();
        row["StartOfChain"].Should().Be("true", "no v1 signature was readable, so T121 starts the v2 chain");
    }

    /// <summary>
    /// After a route-A migration, the first v2 entry of a chain is signed as a chain start (flag N, no
    /// previous signature), the archive manifest anchors the chain as starting in the file, and the
    /// verifier accepts the archive.
    /// </summary>
    [Fact]
    public async Task AfterTheMigration_TheArchiveRecordsEachV2ChainAsStarting_AndVerifies()
    {
        var from = DateTime.UtcNow.AddMinutes(-1);
        await SignAsync(Request(0x4021));
        await SignAsync(Request(0x4022));
        var sale = await SignAsync(Sale());

        var signed = SCU.FR.Abstraction.Helpers.FRReceiptQrCode.SignedContentOf(sale.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data).Split('|');
        signed[^2].Should().Be("N", "T121 carries over no previous signature");
        signed[^1].Should().BeEmpty();

        var bytes = await ReadAsync(JournalTypeFR.ArchiveExport, from.Ticks);

        Journal.FR.Archive.Packaging.ArchiveVerifier.Verify(new MemoryStream(bytes)).Errors.Should().BeEmpty();
        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        using var manifest = JsonDocument.Parse(ReadEntry(zip, Journal.FR.Archive.Packaging.ArchivePackager<object>.ManifestFileName));
        var anchors = manifest.RootElement.GetProperty("chainAnchors").EnumerateArray().ToDictionary(a => a.GetProperty("chain").GetString()!);
        anchors["T"].GetProperty("firstReceiptIdentification").GetString().Should().Be(sale.ftReceiptIdentification);
        anchors["T"].GetProperty("previousSignature").GetString().Should().BeEmpty("the T chain starts in this file");
        anchors["E"].GetProperty("previousSignature").GetString().Should().BeEmpty();
    }

    private static byte[] ReadEntry(System.IO.Compression.ZipArchive zip, string name)
    {
        using var buffer = new MemoryStream();
        using (var stream = zip.GetEntry(name)!.Open())
        {
            stream.CopyTo(buffer);
        }
        return buffer.ToArray();
    }

    private async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
        => JsonSerializer.Deserialize<ReceiptResponse>(await _sign(JsonSerializer.Serialize(request)))!;

    private async Task<byte[]> ReadAsync(JournalTypeFR type, long from = 0)
    {
        var (_, reader) = await _journal(JsonSerializer.Serialize(new JournalRequest { ftJournalType = type.As<JournalType>(), From = from }));
        using var buffer = new MemoryStream();
        await reader.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private static List<Dictionary<string, string>> Rows(byte[] csv)
    {
        var lines = Encoding.UTF8.GetString(csv).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var header = lines[0].Split(';');
        return lines.Skip(1).Select(l => header.Zip(l.Split(';')).ToDictionary(p => p.First, p => p.Second)).ToList();
    }

    private ReceiptRequest Request(long receiptCase) => new()
    {
        ftCashBoxID = _cashBoxId,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString("N")[..8],
        cbReceiptMoment = DateTime.UtcNow,
        Currency = Currency.EUR,
        ftReceiptCase = ((ReceiptCase) receiptCase).WithCountry("FR"),
        cbChargeItems = [],
        cbPayItems = [],
    };

    private ReceiptRequest Sale()
    {
        var request = Request(0x0001);
        request.cbChargeItems = [new ChargeItem { Amount = 12.00m, VATAmount = 2.00m, VATRate = 20m, Quantity = 1, Description = "Cafe", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }];
        request.cbPayItems = [new PayItem { Amount = 12.00m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }];
        return request;
    }
}
