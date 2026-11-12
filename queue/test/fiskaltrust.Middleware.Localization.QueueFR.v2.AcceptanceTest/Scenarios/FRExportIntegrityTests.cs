using System.IO.Pipelines;
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
/// A local archive export checks the chains of the entries it exports (market-fr ADR 0009, #208): a broken link
/// raises JET 90, a gap in a chain's counters JET 95, each as a pending JET event. The export is still delivered, and
/// raises JET 110 (contract §6).
/// </summary>
public class FRExportIntegrityTests : IDisposable
{
    private readonly Guid _queueId = Guid.NewGuid();
    private readonly Guid _cashBoxId = Guid.NewGuid();
    private readonly InMemorySCU _scu = new();
    private Func<string, Task<string>> _signProcessor = null!;
    private Func<string, Task<(System.Net.Mime.ContentType contentType, PipeReader reader)>> _journal = null!;
    private InMemoryLocalizationStorageProvider _storageProvider = null!;

    public FRExportIntegrityTests() => Start();

    public void Dispose() => _scu.Dispose();

    [Fact]
    public async Task IntactChains_RaiseNoIntegrityEvent_OnlyTheExportEvent()
    {
        var from = DateTime.UtcNow;
        await SignAsync(CashSale());
        await SignAsync(CashSale());

        (await ExportAsync(from)).Should().NotBeEmpty();

        (await PendingAsync()).Select(x => x.Type).Should().Equal(new[] { "110-JetPending" }, "every local archive export raises JET 110 (contract §6)");
    }

    /// <summary>A stored entry tampered with afterwards no longer links to the entry before it.</summary>
    [Fact]
    public async Task ATamperedStoredEntry_RaisesAPendingJet90()
    {
        var from = DateTime.UtcNow;
        var first = await SignAsync(CashSale());
        var second = await SignAsync(CashSale());
        await SignAsync(CashSale());
        var stored = await StoredAsync(second);
        stored.response.Should().Contain(ChainedSignature(first));
        // Changed in place: the storage keeps its row and time stamp, only the signed content differs.
        stored.response = stored.response.Replace(ChainedSignature(first), "forged");

        (await ExportAsync(from)).Should().NotBeEmpty("the export is reported on, not refused");

        var pending = await PendingAsync();
        pending.Select(x => x.Type).Should().BeEquivalentTo("90-JetPending", "110-JetPending");
        pending.Single(x => x.Type == "90-JetPending").DataJson.Should().Contain("chain=T").And.Contain(second.ftReceiptIdentification);
        (await SignAsync(CashSale())).ftState.IsFlag(StateFlags.MessageIsPending).Should().BeTrue();
    }

    /// <summary>A stored entry deleted afterwards leaves a gap in its chain's counters.</summary>
    [Fact]
    public async Task ADeletedStoredEntry_RaisesAPendingJet95()
    {
        var from = DateTime.UtcNow;
        await SignAsync(CashSale());
        var second = await SignAsync(CashSale());
        await SignAsync(CashSale());
        // The in-memory storage has no delete: a time stamp outside every read takes the entry out of the export.
        (await StoredAsync(second)).TimeStamp = 0;

        (await ExportAsync(from)).Should().NotBeEmpty("the export is reported on, not refused");

        var pending = await PendingAsync();
        pending.Select(x => x.Type).Should().BeEquivalentTo("95-JetPending", "110-JetPending");
        pending.Single(x => x.Type == "95-JetPending").DataJson.Should().Contain("chain=T").And.Contain("expected=2").And.Contain("found=3");
        (await SignAsync(CashSale())).ftState.IsFlag(StateFlags.MessageIsPending).Should().BeTrue();
    }

    /// <summary>
    /// JET 110 (R19 "Exportation de données") on a local export of fiscal data (contract §6): the archive export
    /// (0x4652_2000_0000_0010) and every journal export (001-00F), returned on the next zero receipt.
    /// </summary>
    [Fact]
    public async Task AnArchiveExport_RaisesAPendingJet110_ReturnedOnTheNextZeroReceipt()
    {
        var from = DateTime.UtcNow;
        await SignAsync(CashSale());

        (await ExportAsync(from)).Should().NotBeEmpty();

        var pending = (await PendingAsync()).Should().ContainSingle().Which;
        pending.Type.Should().Be("110-JetPending");
        pending.DataJson.Should().Contain("Exportation de donn").And.Contain("ftJournalType=0x4652200000000010").And.Contain("file=");
        var zero = await SignAsync(Request(ReceiptCase.ZeroReceipt0x2000));
        zero.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent)).Select(x => JsonDocument.Parse(x.Data).RootElement.GetProperty("FR").GetProperty("Code").GetString())
            .Should().Equal("110");
    }

    [Theory]
    [InlineData(JournalTypeFR.Ticket, "0x4652200000000001", "journal-T-")]
    [InlineData(JournalTypeFR.Log, "0x4652200000000009", "journal-L-")]
    public async Task AJournalExport_RaisesJet110(JournalTypeFR journalType, string code, string file)
    {
        var from = DateTime.UtcNow;
        await SignAsync(CashSale());
        var jetEvent = Request(ReceiptCase.ProtocolTechnicalEvent0x3001);
        jetEvent.ftReceiptCaseData = "{\"FR\":{\"Code\":\"900\",\"Message\":\"editor event\"}}";
        await SignAsync(jetEvent);

        await JournalAsync(journalType, from);

        (await PendingAsync()).Should().ContainSingle().Which.DataJson.Should().Contain($"ftJournalType={code}").And.Contain($"file={file}");
    }

    [Fact]
    public async Task AJournalExportWithNoEntry_RaisesNothing()
    {
        var from = DateTime.UtcNow;
        await SignAsync(CashSale());

        await JournalAsync(JournalTypeFR.Invoice, from);

        (await PendingAsync()).Should().BeEmpty("no invoice lies in the period, so no data was exported");
    }

    [Fact]
    public async Task ARefusedArchiveExport_RaisesNothing()
    {
        await SignAsync(CashSale());
        var from = DateTime.UtcNow.AddSeconds(1);

        var act = () => ExportAsync(from);

        await act.Should().ThrowAsync<Exception>("the period holds no receipt");
        (await PendingAsync()).Should().BeEmpty();
    }

    private async Task<byte[]> JournalAsync(JournalTypeFR journalType, DateTime from)
    {
        var (_, reader) = await _journal(JsonSerializer.Serialize(new JournalRequest { ftJournalType = journalType.As<JournalType>(), From = from.Ticks }));
        using var buffer = new MemoryStream();
        await reader.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private void Start()
    {
        var queueFR = new ftQueueFR { ftQueueFRId = _queueId, CashBoxIdentification = "cashbox-fr", Siret = "12345678901234" };
        var configuration = new Dictionary<string, object>
        {
            { "FRLocalDevelopment", true },
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

    private async Task<ftQueueItem> StoredAsync(ReceiptResponse response)
        => (await (await _storageProvider.CreateMiddlewareQueueItemRepository()).GetAsync()).Single(q => q.ftQueueItemId == response.ftQueueItemID);

    private async Task<List<ftActionJournal>> PendingAsync()
        => (await (await _storageProvider.CreateMiddlewareActionJournalRepository()).GetAsync()).Where(x => x.Type?.EndsWith("-JetPending") == true).ToList();

    private async Task<byte[]> ExportAsync(DateTime from)
    {
        var (_, reader) = await _journal(JsonSerializer.Serialize(new JournalRequest { ftJournalType = JournalTypeFR.ArchiveExport.As<JournalType>(), From = from.Ticks }));
        using var buffer = new MemoryStream();
        await reader.CopyToAsync(buffer);
        return buffer.ToArray();
    }

    private async Task<ReceiptResponse> SignAsync(ReceiptRequest request)
    {
        var response = JsonSerializer.Deserialize<ReceiptResponse>(await _signProcessor(JsonSerializer.Serialize(request)))!;
        response.ftState.IsState(State.Error).Should().BeFalse();
        return response;
    }

    private static string ChainedSignature(ReceiptResponse response)
    {
        var signature = response.ftSignatures.First(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;
        return signature[(signature.LastIndexOf('.') + 1)..];
    }

    private ReceiptRequest Request(ReceiptCase receiptCase) => new()
    {
        ftCashBoxID = _cashBoxId,
        cbTerminalID = "1",
        cbReceiptReference = Guid.NewGuid().ToString("N")[..8],
        cbReceiptMoment = DateTime.UtcNow,
        Currency = Currency.EUR,
        ftReceiptCase = receiptCase.WithCountry("FR"),
        cbChargeItems = [],
        cbPayItems = [],
    };

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
