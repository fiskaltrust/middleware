using System.IO.Compression;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Journal.FR.Archive.Packaging;
using fiskaltrust.Middleware.Journal.FR.Archive.V2;
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
/// The export reads each entry's chain links from what the SCU signed (archive-v2 §1.1, ADR 0001) and never replays
/// the queue (ADR 0012): a chain continued from v1.2 keeps its link, an entry it cannot read fails the export, and a
/// large period is read as a stream.
/// </summary>
public class FRArchiveSignedLinksTests : IDisposable
{
    private const string V12Signature = "v12-T41-signature";

    private readonly Guid _queueId = Guid.NewGuid();
    private readonly Guid _cashBoxId = Guid.NewGuid();
    private readonly InMemorySCU _scu = new();
    private Func<string, Task<string>> _signProcessor = null!;
    private Func<string, Task<(System.Net.Mime.ContentType contentType, PipeReader reader)>> _journal = null!;
    private InMemoryLocalizationStorageProvider _storageProvider = null!;

    public void Dispose() => _scu.Dispose();

    private void Start(ftQueueFR queueFR)
    {
        queueFR.ftQueueFRId = _queueId;
        queueFR.CashBoxIdentification = "cashbox-fr";
        queueFR.Siret = "12345678901234";
        var configuration = new Dictionary<string, object>
        {
            { "cashboxid", _cashBoxId },
            { "init_ftCashBox", JsonSerializer.Serialize(new ftCashBox { ftCashBoxId = _cashBoxId, TimeStamp = DateTime.UtcNow.Ticks }) },
            { "init_ftQueue", JsonSerializer.Serialize(new List<ftQueue> { new() { ftQueueId = _queueId, ftCashBoxId = _cashBoxId, StartMoment = DateTime.UtcNow, CountryCode = "FR" } }) },
            { "init_ftQueueFR", JsonSerializer.Serialize(new List<ftQueueFR> { queueFR }) },
            { "init_ftSignaturCreationUnitFR", JsonSerializer.Serialize(new List<ftSignaturCreationUnitFR>()) },
            { "init_masterData", JsonSerializer.Serialize(new MasterDataConfiguration { Account = new AccountMasterData { AccountId = Guid.NewGuid(), AccountName = "fiskaltrust SARL", Country = "FR" } }) },
        };
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _storageProvider = new InMemoryLocalizationStorageProvider(_queueId, configuration, loggerFactory);
        var bootstrapper = new QueueFRBootstrapper(_queueId, loggerFactory, configuration, _scu, _storageProvider);
        _signProcessor = bootstrapper.RegisterForSign();
        _journal = bootstrapper.RegisterForJournal();
    }

    [Fact]
    public async Task HalfMigration_TheFirstV2Ticket_CarriesTheV12SignatureItWasChainedOn()
    {
        // ADR 0003: the v2 queue takes over the v1.2 chain state; its T chain goes on at 42 after the v1.2 T41.
        Start(new ftQueueFR { TNumerator = 41, TLastSignature = V12Signature });
        await StoreV1PredecessorAsync();
        var from = DateTime.UtcNow;
        var first = await SignAsync(CashSale());

        var rows = Rows(await ReadAsync(JournalTypeFR.Ticket));

        rows.Should().ContainSingle().Which["ftReceiptIdentification"].Should().Be(first.ftReceiptIdentification);
        rows[0]["ftReceiptIdentification"].Should().EndWith("T42");
        rows[0]["StartOfChain"].Should().Be("false", "the entry continues the v1.2 chain");
        rows[0]["PreviousSignature"].Should().Be(V12Signature);

        var bytes = await ReadAsync(JournalTypeFR.ArchiveExport, from.Ticks);
        using var zip = new ZipArchive(new MemoryStream(bytes));
        using var subArchive = new ZipArchive(zip.Entries.Single(e => e.Name.StartsWith("subarchive_")).Open());
        using var manifest = JsonDocument.Parse(ReadEntry(subArchive, ArchivePackager<object>.ManifestFileName));
        var anchor = manifest.RootElement.GetProperty("chainAnchors").EnumerateArray().Single(a => a.GetProperty("chain").GetString() == "T");
        anchor.GetProperty("previousSignature").GetString().Should().Be(V12Signature, "a chain anchor is empty only when the entry starts the chain (archive-v2 §10.2)");
    }

    [Fact]
    public async Task Export_FailsOnAnEntryOfThePeriodItCannotRead()
    {
        Start(new ftQueueFR());
        var from = DateTime.UtcNow;
        await SignAsync(CashSale());
        var broken = await SignAsync(CashSale());
        var repository = await _storageProvider.CreateMiddlewareQueueItemRepository();
        var stored = (await repository.GetAsync()).Single(q => q.ftQueueItemId == broken.ftQueueItemID);
        stored.response = stored.response[..(stored.response.Length / 2)];
        await repository.InsertOrUpdateAsync(stored);

        var journal = () => ReadAsync(JournalTypeFR.Ticket);
        var archive = () => ReadAsync(JournalTypeFR.ArchiveExport, from.Ticks);

        (await journal.Should().ThrowAsync<ArchiveEntryException>()).Which.QueueRow.Should().Be(stored.ftQueueRow);
        await archive.Should().ThrowAsync<ArchiveEntryException>();
    }

    [Fact]
    public async Task Export_OfALargePeriod_KeepsEveryEntryInQueueOrderAndChained()
    {
        Start(new ftQueueFR());
        const int count = 300;
        var from = DateTime.UtcNow;
        var signed = new List<ReceiptResponse>(count);
        for (var i = 0; i < count; i++)
        {
            signed.Add(await SignAsync(CashSale()));
        }

        var rows = Rows(await ReadAsync(JournalTypeFR.Ticket));

        rows.Select(r => r["ftReceiptIdentification"]).Should().Equal(signed.Select(s => s.ftReceiptIdentification));
        rows[0]["StartOfChain"].Should().Be("true");
        for (var i = 1; i < count; i++)
        {
            rows[i]["PreviousSignature"].Should().Be(ChainedSignature(signed[i - 1]));
        }

        var bytes = await ReadAsync(JournalTypeFR.ArchiveExport, from.Ticks);
        using var zip = new ZipArchive(new MemoryStream(bytes));
        using var subArchive = new ZipArchive(zip.Entries.Single(e => e.Name.StartsWith("subarchive_")).Open());
        using var manifest = JsonDocument.Parse(ReadEntry(subArchive, ArchivePackager<object>.ManifestFileName));
        manifest.RootElement.GetProperty("recordCount").GetInt32().Should().Be(count);
    }

    [Fact]
    public async Task ArchiveReceipt_IsRefused_WhenThePreviousArchiveReceiptIsNotInStorage()
    {
        // The chain state names a previous archive receipt that storage no longer returns (a purged or migrated store):
        // signing would collapse the range to the receipt itself and leave every record since then in no signed range.
        Start(new ftQueueFR { ALastQueueItemId = Guid.NewGuid(), ANumerator = 3, ALastSignature = "a3-signature" });
        await SignAsync(CashSale());

        var request = CashSale();
        request.ftReceiptCase = ((ReceiptCase) 0x3011).WithCountry("FR"); // 0x3011: archive receipt
        request.cbChargeItems = [];
        request.cbPayItems = [];
        var response = JsonSerializer.Deserialize<ReceiptResponse>(await _signProcessor(JsonSerializer.Serialize(request)))!;

        response.ftState.IsState(State.Error).Should().BeTrue();
        response.ftSignatures.Should().NotContain(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature));
        JsonSerializer.Serialize(response.ftSignatures).Should().Contain("previous archive receipt");
        Rows(await ReadAsync(JournalTypeFR.Archive)).Should().BeEmpty("no archive receipt was signed");
    }

    /// <summary>A v1.2 ticket stored before the handover: v1 signature types (0x4652_0000_0000_0001, v1 receipt signature).</summary>
    private async Task StoreV1PredecessorAsync()
    {
        var repository = await _storageProvider.CreateMiddlewareQueueItemRepository();
        var moment = DateTime.UtcNow.AddMinutes(-1);
        await repository.InsertOrUpdateAsync(new ftQueueItem
        {
            ftQueueItemId = Guid.NewGuid(),
            ftQueueId = _queueId,
            ftQueueRow = 1,
            ftQueueMoment = moment,
            // 0x4652_0000_0000_0001: v1 ticket receipt case; 0x4652_0000_0000_0000: v1 ready state.
            request = "{\"ftReceiptCase\":" + 0x4652_0000_0000_0001L + "}",
            response = "{\"ftReceiptIdentification\":\"ft0#T41\",\"ftReceiptMoment\":\"" + moment.ToString("o") + "\",\"ftState\":" + 0x4652_0000_0000_0000L + ","
                + "\"ftSignatures\":[{\"ftSignatureFormat\":1,\"ftSignatureType\":" + 0x4652_0000_0000_0001L + ",\"Caption\":\"x\",\"Data\":\"" + V12Signature + "\"}]}",
        });
    }

    private async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
    {
        var response = JsonSerializer.Deserialize<ReceiptResponse>(await _signProcessor(JsonSerializer.Serialize(request)))!;
        response.ftState.IsState(State.Error).Should().BeFalse();
        return response;
    }

    private async Task<byte[]> ReadAsync(JournalTypeFR type, long from = 0, long to = 0)
    {
        var (_, reader) = await _journal(JsonSerializer.Serialize(new JournalRequest { ftJournalType = type.As<JournalType>(), From = from, To = to }));
        using var buffer = new MemoryStream();
        await reader.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private static byte[] ReadEntry(ZipArchive zip, string name)
    {
        using var buffer = new MemoryStream();
        using (var stream = zip.GetEntry(name)!.Open())
        {
            stream.CopyTo(buffer);
        }
        return buffer.ToArray();
    }

    private static List<Dictionary<string, string>> Rows(byte[] csv)
    {
        var lines = Encoding.UTF8.GetString(csv).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var header = lines[0].Split(';');
        return lines.Skip(1).Select(l => header.Zip(l.Split(';')).ToDictionary(p => p.First, p => p.Second)).ToList();
    }

    private static string ChainedSignature(ReceiptResponse response)
    {
        var signature = response.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;
        return signature[(signature.LastIndexOf('.') + 1)..];
    }

    private ReceiptRequest CashSale() => new()
    {
        ftCashBoxID = _cashBoxId,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString("N")[..8],
        cbReceiptMoment = DateTime.UtcNow,
        Currency = Currency.EUR,
        ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"),
        cbChargeItems =
        [
            new ChargeItem { Amount = 12.00m, VATAmount = 2.00m, VATRate = 20m, Quantity = 1, Description = "Cafe", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
        ],
        cbPayItems =
        [
            new PayItem { Amount = 12.00m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") },
        ],
    };
}
