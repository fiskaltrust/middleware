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
/// raises JET 90, a gap in a chain's counters JET 95, each as a pending JET event. The export is still delivered.
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
    public async Task IntactChains_RaiseNothing()
    {
        var from = DateTime.UtcNow;
        await SignAsync(CashSale());
        await SignAsync(CashSale());

        (await ExportAsync(from)).Should().NotBeEmpty();

        (await PendingAsync()).Should().BeEmpty();
        (await SignAsync(CashSale())).ftState.IsFlag(StateFlags.MessageIsPending).Should().BeFalse();
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
        pending.Select(x => x.Type).Should().Equal("90-JetPending");
        pending[0].DataJson.Should().Contain("chain=T").And.Contain(second.ftReceiptIdentification);
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
        pending.Select(x => x.Type).Should().Equal("95-JetPending");
        pending[0].DataJson.Should().Contain("chain=T").And.Contain("expected=2").And.Contain("found=3");
        (await SignAsync(CashSale())).ftState.IsFlag(StateFlags.MessageIsPending).Should().BeTrue();
    }

    private void Start()
    {
        var queueFR = new ftQueueFR { ftQueueFRId = _queueId, CashBoxIdentification = "cashbox-fr", Siret = "12345678901234" };
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
