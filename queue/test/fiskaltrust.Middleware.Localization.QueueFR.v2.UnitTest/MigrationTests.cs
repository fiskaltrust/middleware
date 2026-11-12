using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// The half migration of a v1.2 queue to v2 (market-fr ADR 0003): 0x4021 checks and signs on E,
/// 0x4022 checks again, signs the migration event on E and hands the v1 state over once.
/// </summary>
public class MigrationTests
{
    private static readonly DateTime LastDailyClosing = new(2026, 11, 2, 22, 0, 0, DateTimeKind.Utc);

    private readonly FakeFRSSCD _sscd = new();
    private readonly InMemoryQueueItemRepository _queueItems = new();
    private readonly StoredChainState _state;
    private readonly FRSigningPipeline _pipeline;
    private readonly FRMigration _migration;

    public MigrationTests()
    {
        _state = new StoredChainState(V1Queue());
        _pipeline = new FRSigningPipeline(_sscd, _state.Provider());
        _migration = new FRMigration(_pipeline, _state.Provider(), new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(_queueItems)));
    }

    /// <summary>A v1.2 queue at the cut: day closed, a v1.2 archive receipt, hashes kept per chain.</summary>
    private static ftQueueFR V1Queue() => new()
    {
        ftQueueFRId = Guid.NewGuid(),
        TNumerator = 120,
        TLastHash = "v1-ticket-hash",
        TTotalizer = 1000.00m,
        GNumerator = 30,
        GLastHash = "v1-grand-total-hash",
        GLastDayMoment = LastDailyClosing,
        GMonthTotalizer = 300.00m,
        GYearTotalizer = 900.00m,
        ANumerator = 3,
        ALastHash = "v1-archive-hash",
        ALastQueueItemId = Guid.NewGuid(),
        ALastMoment = LastDailyClosing.AddMinutes(5),
    };

    private ProcessCommandRequest Request(long receiptCase, bool training = false, decimal amount = 0m)
    {
        var caseValue = ((ReceiptCase) receiptCase).WithCountry("FR");
        return new ProcessCommandRequest(
            new ftQueue { ftQueueId = _state.QueueId, StartMoment = DateTime.UtcNow.AddYears(-2) },
            new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = training ? caseValue.WithFlag(ReceiptCaseFlags.Training) : caseValue,
                Currency = Currency.EUR,
                cbReceiptMoment = DateTime.UtcNow,
                cbChargeItems = amount == 0 ? [] : [new ChargeItem { Amount = amount, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }],
                cbPayItems = amount == 0 ? [] : [new PayItem { Amount = amount, Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }],
            },
            new ReceiptResponse { ftQueueID = _state.QueueId, ftQueueItemID = Guid.NewGuid(), ftReceiptIdentification = "ft1#", ftReceiptMoment = DateTime.UtcNow, ftState = (State) StateFR.Success, ftSignatures = [] });
    }

    private async Task<ReceiptResponse> InitiateAsync()
    {
        var response = (await _migration.InitiateAsync(Request(0x4021)))!.receiptResponse;
        await _state.CommitAsync();
        return response;
    }

    private async Task<ReceiptResponse> FinishAsync()
    {
        var response = (await _migration.FinishAsync(Request(0x4022)))!.receiptResponse;
        await _state.CommitAsync();
        return response;
    }

    private async Task Change(Action<ftQueueFR> change)
    {
        var queueFR = await _state.ReadAsync();
        change(queueFR);
        await _state.Repository.InsertOrUpdateQueueFRAsync(queueFR);
    }

    [Fact]
    public async Task WithoutAV12ArchiveReceipt_TheMigrationIsRefused()
    {
        await Change(x => x.ALastQueueItemId = null);

        var response = await InitiateAsync();

        response.ftState.IsState(State.Error).Should().BeTrue();
        response.ftSignatures.Should().Contain(x => x.Data.Contains("archive receipt (0x3011)"));
        _sscd.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task WithTheV1DayOpen_TheMigrationIsRefused()
    {
        await Change(x => x.GDayTotalizer = 40m);

        (await InitiateAsync()).ftState.IsState(State.Error).Should().BeTrue();
        (await FinishAsync()).ftState.IsState(State.Error).Should().BeTrue();
        _sscd.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task WithPendingV1Messages_TheMigrationIsRefused()
    {
        await Change(x => x.MessageCount = 1);

        (await FinishAsync()).ftSignatures.Should().Contain(x => x.Data.Contains("pending message"));
        _sscd.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task InitiateMigration_SignsOnTheTechnicalChain_AndChangesNothingElse()
    {
        var response = await InitiateAsync();

        response.ftReceiptIdentification.Should().Be("ft1#E1");
        var stored = await _state.ReadAsync();
        stored.GYearTotalizer.Should().Be(900m);
        FRV1Handover.AwaitsMigration(stored).Should().BeTrue("only 0x4022 migrates");
    }

    [Fact]
    public async Task FinishMigration_SignsTheMigrationEvent_AndHandsTheV1StateOverOnce()
    {
        await InitiateAsync();
        var finish = await FinishAsync();

        finish.ftReceiptIdentification.Should().Be("ft1#E2");
        var migrationEvent = FRStateData.Migration(Stored(finish))!;
        migrationEvent.Chains.Should().ContainSingle(x => x.Chain == "T").Which.Should().BeEquivalentTo(new { Counter = 120L, LastHash = "v1-ticket-hash", LastSignature = (string?) null });
        migrationEvent.Chains.Should().Contain(x => x.Chain == "G" && x.LastHash == "v1-grand-total-hash");
        migrationEvent.Chains.Should().Contain(x => x.Chain == "A" && x.Counter == 3);

        var stored = await _state.ReadAsync();
        FRV1Handover.AwaitsMigration(stored).Should().BeFalse();
        FRStateV2.Read(stored).MigratedAt.Should().Be(finish.ftReceiptMoment);
        stored.GYearTotalizer.Should().Be(1200m, "the open v1 month is folded into the year once");

        var again = await FinishAsync();
        again.ftState.IsState(State.Error).Should().BeTrue("the queue is migrated");
        (await _state.ReadAsync()).GYearTotalizer.Should().Be(1200m);
    }

    [Fact]
    public async Task AfterTheMigration_WithNoReadableV1Signature_TheV2ChainStartsFresh()
    {
        await InitiateAsync();
        await FinishAsync();

        var sale = (await _pipeline.SignAsync(Request(0x0001, amount: 10m))).receiptResponse;

        sale.ftReceiptIdentification.Should().Be("ft1#T121", "the v1 counter continues");
        _sscd.Calls.Last().previousSignature.Should().BeNull("route A: the signed migration event is the link to v1");
    }

    [Fact]
    public async Task AfterTheMigration_WithTheV1SignatureReadable_TheV2ChainContinuesIt()
    {
        await StoreV1EntryAsync(row: 1, "ft77#T120", "eyJhbGciOiJFUzI1NiJ9.eyJ9.djEtdGlja2V0LXNpZw");

        await InitiateAsync();
        var finish = await FinishAsync();
        FRStateData.Migration(Stored(finish))!.Chains.Single(x => x.Chain == "T").LastSignature.Should().Be("djEtdGlja2V0LXNpZw");

        await _pipeline.SignAsync(Request(0x0001, amount: 10m));

        _sscd.Calls.Last().previousSignature.Should().Be("djEtdGlja2V0LXNpZw");
    }

    [Fact]
    public async Task ATrainingMigrationReceipt_IsRefused()
    {
        var response = (await _migration.InitiateAsync(Request(0x4021, training: true)))!.receiptResponse;

        response.ftState.IsState(State.Error).Should().BeTrue();
        _sscd.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task UntilMigrated_AV12QueueAcceptsOnlyTheMigrationReceipts()
    {
        var processed = 0;
        var gate = FRMigrationGate.Wrap(_state.Provider(), (request, response, queue, item) => { processed++; return Task.FromResult((response, new List<ftActionJournal>())); });

        var sale = Request(0x0001, amount: 10m);
        var (refused, _) = await gate(sale.ReceiptRequest, sale.ReceiptResponse, sale.queue, new ftQueueItem());
        refused.ftState.IsState(State.Error).Should().BeTrue();
        processed.Should().Be(0);

        var initiate = Request(0x4021);
        await gate(initiate.ReceiptRequest, initiate.ReceiptResponse, initiate.queue, new ftQueueItem());
        processed.Should().Be(1);

        await InitiateAsync();
        await FinishAsync();
        var later = Request(0x0001, amount: 10m);
        await gate(later.ReceiptRequest, later.ReceiptResponse, later.queue, new ftQueueItem());
        processed.Should().Be(2, "a migrated queue accepts every receipt");
    }

    [Fact]
    public async Task AQueueStartedByV2_IsNeitherHeldNorMigrated()
    {
        var v2 = new StoredChainState();
        var gate = FRMigrationGate.Wrap(v2.Provider(), (request, response, queue, item) => Task.FromResult((response, new List<ftActionJournal>())));
        var sale = Request(0x0001, amount: 10m);

        var (response, _) = await gate(sale.ReceiptRequest, sale.ReceiptResponse, sale.queue, new ftQueueItem());

        response.ftState.IsState(State.Error).Should().BeFalse();
        var refused = (await new FRMigration(new FRSigningPipeline(_sscd, v2.Provider()), v2.Provider()).FinishAsync(Request(0x4022)))!.receiptResponse;
        refused.ftState.IsState(State.Error).Should().BeTrue("there is no v1.2 state to migrate");
    }

    /// <summary>A lost store of the finish-migration receipt is replayed: the handover is applied from its signed event.</summary>
    [Fact]
    public async Task AReplayedFinishMigrationEntry_HandsTheV1StateOver()
    {
        await InitiateAsync();
        var finish = Request(0x4022);
        var response = (await _migration.FinishAsync(finish))!.receiptResponse;
        response.ftSignatures.Add(new SignatureItem { ftSignatureType = (SignatureType) (long) SignatureTypeFR.ReceiptSignature, ftSignatureFormat = SignatureFormat.QRCode, Data = "data.sig-2" });
        var item = new ftQueueItem { ftQueueItemId = response.ftQueueItemID, request = JsonSerializer.Serialize(finish.ReceiptRequest), response = JsonSerializer.Serialize(response) };
        var queueFR = V1Queue();
        queueFR.ENumerator = 1;

        var (kind, entry) = FRChainRepair.Read(item);
        kind.Should().Be(FRChainRepair.Kind.Signed);
        FRChainRepair.Apply(queueFR, entry!);

        FRStateV2.Read(queueFR).MigratedAt.Should().NotBeNull();
        queueFR.GYearTotalizer.Should().Be(1200m);
    }

    private static ReceiptResponse Stored(ReceiptResponse response) => JsonSerializer.Deserialize<ReceiptResponse>(JsonSerializer.Serialize(response))!;

    private async Task StoreV1EntryAsync(long row, string identification, string jws)
    {
        var request = new ReceiptRequest { ftReceiptCase = (ReceiptCase) 0x4652_0000_0000_0001, cbChargeItems = [], cbPayItems = [] };
        var response = new ReceiptResponse
        {
            ftReceiptIdentification = identification,
            ftState = (State) 0x4652_0000_0000_0000,
            ftSignatures = [new SignatureItem { ftSignatureType = (SignatureType) 0x4652_0000_0000_0001, ftSignatureFormat = SignatureFormat.QRCode, Data = jws }],
        };
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem
        {
            ftQueueItemId = Guid.NewGuid(),
            ftQueueRow = row,
            ftQueueMoment = LastDailyClosing.AddHours(-1),
            TimeStamp = LastDailyClosing.AddHours(-1).Ticks,
            request = JsonSerializer.Serialize(request),
            response = JsonSerializer.Serialize(response),
        });
    }
}
