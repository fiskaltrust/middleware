using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// No signed entry is numbered twice or loses its chain link, whatever write fails or when the
/// process stops: the chain state in ftQueueFR and the stored queue items agree after any failure,
/// and the next request repairs a lag (market-fr ADR 0012).
/// </summary>
public class ChainStateRepairTests
{
    private readonly FakeFRSSCD _sscd = new();
    private readonly Guid _queueId = Guid.NewGuid();
    private readonly CopyingConfigurationRepository _configuration = new();
    private readonly InMemoryQueueItemRepository _queueItems = new();
    private readonly Mock<IQueueStorageProvider> _inner = new();
    private readonly ftQueue _queue;

    public ChainStateRepairTests()
    {
        _queue = new ftQueue { ftQueueId = _queueId, StartMoment = DateTime.UtcNow.AddDays(-1) };
        _configuration.InsertOrUpdateQueueFRAsync(new ftQueueFR { ftQueueFRId = _queueId, CashBoxIdentification = "cb" }).GetAwaiter().GetResult();
        _inner.Setup(x => x.ReserveNextQueueItem(It.IsAny<ReceiptRequest>())).ReturnsAsync((ReceiptRequest r) => new ftQueueItem { ftQueueItemId = Guid.NewGuid(), ftQueueId = _queueId, request = JsonSerializer.Serialize(r) });
        _inner.Setup(x => x.GetQueueAsync()).ReturnsAsync(() => _queue);
    }

    private FRChainStateProvider NewChains() => new(new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(_configuration.Object)), _queueId);

    private readonly InMemoryActionJournalRepository _actionJournals = new();

    private FRQueueStorageProvider Storage(FRChainStateProvider chains)
        => new(_inner.Object, chains, new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(_queueItems)),
            new AsyncLazy<IMiddlewareActionJournalRepository>(() => Task.FromResult<IMiddlewareActionJournalRepository>(_actionJournals)));

    private static ReceiptRequest TicketRequest(decimal amount = 10m, ReceiptCase receiptCase = ReceiptCase.PointOfSaleReceipt0x0001) => new()
    {
        ftCashBoxID = Guid.NewGuid(),
        ftReceiptCase = receiptCase.WithCountry("FR"),
        Currency = Currency.EUR,
        cbReceiptMoment = DateTime.UtcNow,
        cbChargeItems = amount == 0 ? [] : [new ChargeItem { Amount = amount, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }],
        cbPayItems = amount == 0 ? [] : [new PayItem { Amount = amount, Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }],
    };

    private ProcessCommandRequest Command(ReceiptRequest request, ftQueueItem queueItem) => new(
        _queue,
        request,
        new ReceiptResponse
        {
            ftQueueID = _queueId,
            ftQueueItemID = queueItem.ftQueueItemId,
            ftReceiptIdentification = "ft1#",
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) StateFR.Success,
            ftSignatures = [],
        });

    /// <summary>One sign request as SignProcessor runs it through the FR storage: reserve, sign, finish.</summary>
    private async Task<ReceiptResponse> RunAsync(FRSigningPipeline pipeline, FRQueueStorageProvider storage, ReceiptRequest request)
    {
        var queueItem = await storage.ReserveNextQueueItem(request);
        var response = (await pipeline.SignAsync(Command(request, queueItem))).receiptResponse;
        await storage.FinishQueueItem(queueItem, response);
        return response;
    }

    private void InnerStoresTheItem(bool thenThrow)
        => _inner.Setup(x => x.FinishQueueItem(It.IsAny<ftQueueItem>(), It.IsAny<ReceiptResponse>()))
            .Returns(async (ftQueueItem item, ReceiptResponse response) =>
            {
                item.response = JsonSerializer.Serialize(response);
                await _queueItems.InsertOrUpdateAsync(item);
                if (thenThrow)
                {
                    throw new IOException("ftQueue write timed out");
                }
            });

    /// <summary>the queue item is stored, then the ftQueue write fails; the chain must keep the entry.</summary>
    [Fact]
    public async Task WhenTheWriteAfterTheStoredItemFails_TheNumberIsNotReused()
    {
        var chains = NewChains();
        var pipeline = new FRSigningPipeline(_sscd, chains);
        var storage = Storage(chains);

        InnerStoresTheItem(thenThrow: true);
        var failing = () => RunAsync(pipeline, storage, TicketRequest());
        await failing.Should().ThrowAsync<IOException>();

        InnerStoresTheItem(thenThrow: false);
        var next = await RunAsync(pipeline, storage, TicketRequest());

        next.ftReceiptIdentification.Should().Be("ft1#T2", "T1 is stored, so its number is taken");
        _sscd.Calls.Last().previousSignature.Should().Be("sig-1", "the chain covers the stored T1");
        (await _configuration.GetQueueFRAsync(_queueId)).TNumerator.Should().Be(2);
    }

    /// <summary>a restart finds ftQueueFR behind the last stored signed item and rolls it forward.</summary>
    [Fact]
    public async Task AtStartUp_AChainStateBehindTheStoredItems_IsRolledForward()
    {
        await StoreSignedAsync(TicketRequest(10m), "ft7#T1", "stored-sig-1", row: 1);

        var chains = NewChains();
        var next = await RunAsync(new FRSigningPipeline(_sscd, chains), Storage(chains), TicketRequest(4m));

        next.ftReceiptIdentification.Should().Be("ft1#T2");
        _sscd.Calls.Single().previousSignature.Should().Be("stored-sig-1");
        var stored = await _configuration.GetQueueFRAsync(_queueId);
        stored.TNumerator.Should().Be(2);
        stored.TTotalizer.Should().Be(14m, "the stored T1 is counted once, then T2");
        stored.GDayTotalizer.Should().Be(14m);
    }

    [Fact]
    public async Task AtStartUp_AChainStateInStep_IsLeftAlone()
    {
        await StoreSignedAsync(TicketRequest(10m), "ft7#T1", "stored-sig-1", row: 1);
        var queueFR = await _configuration.GetQueueFRAsync(_queueId);
        queueFR.TNumerator = 1;
        queueFR.TLastSignature = "stored-sig-1";
        queueFR.TTotalizer = 10m;
        await _configuration.InsertOrUpdateQueueFRAsync(queueFR);

        var chains = NewChains();
        await RunAsync(new FRSigningPipeline(_sscd, chains), Storage(chains), TicketRequest(4m));

        (await _configuration.GetQueueFRAsync(_queueId)).TTotalizer.Should().Be(14m, "nothing is replayed twice");
    }

    /// <summary>the opening receipt activates the queue only once its signed item is stored.</summary>
    [Fact]
    public async Task InitialOperation_ActivatesTheQueueOnlyOnceItsSignedItemIsStored()
    {
        _queue.StartMoment = null;
        var chains = NewChains();
        var pipeline = new FRSigningPipeline(_sscd, chains);
        var storage = Storage(chains);
        var lifecycle = new LifecycleCommandProcessorFR(_sscd, pipeline, storage);
        var request = TicketRequest(0m, ReceiptCase.InitialOperationReceipt0x4001);

        _inner.Setup(x => x.FinishQueueItem(It.IsAny<ftQueueItem>(), It.IsAny<ReceiptResponse>())).ThrowsAsync(new IOException("disk full"));
        var queueItem = await storage.ReserveNextQueueItem(request);
        var response = (await lifecycle.InitialOperationReceipt0x4001Async(Command(request, queueItem))).receiptResponse;
        _inner.Verify(x => x.ActivateQueueAsync(), Times.Never, "nothing is stored yet");
        var finish = () => storage.FinishQueueItem(queueItem, response);
        await finish.Should().ThrowAsync<IOException>();
        _inner.Verify(x => x.ActivateQueueAsync(), Times.Never, "the opening entry was not stored");

        InnerStoresTheItem(thenThrow: false);
        queueItem = await storage.ReserveNextQueueItem(request);
        response = (await lifecycle.InitialOperationReceipt0x4001Async(Command(request, queueItem))).receiptResponse;
        await storage.FinishQueueItem(queueItem, response);
        _inner.Verify(x => x.ActivateQueueAsync(), Times.Once);
    }

    [Fact]
    public async Task AtStartUp_AStoredOpeningEntryWithoutActivation_ActivatesTheQueue()
    {
        _queue.StartMoment = null;
        await StoreSignedAsync(TicketRequest(0m, ReceiptCase.InitialOperationReceipt0x4001), "ft1#E1", "stored-sig-1", row: 1);

        var chains = NewChains();
        await Storage(chains).ReserveNextQueueItem(TicketRequest());

        _inner.Verify(x => x.ActivateQueueAsync(), Times.Once);
    }

    /// <summary>closing late signing must not unsign a receipt the SCU already signed.</summary>
    [Fact]
    public async Task ClosingLateSigning_WhenTheFirstReceiptCannotBeRead_StillKeepsTheSignedReceipt()
    {
        var failingItems = new Mock<IMiddlewareQueueItemRepository>();
        failingItems.Setup(x => x.GetAsync(It.IsAny<Guid>())).ThrowsAsync(new TimeoutException("storage timeout"));
        var chains = NewChains();
        var pipeline = new FRSigningPipeline(_sscd, chains, new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(failingItems.Object)));

        var lateSigned = TicketRequest();
        lateSigned.ftReceiptCase = lateSigned.ftReceiptCase.WithFlag(ReceiptCaseFlags.LateSigning);
        await pipeline.SignAsync(Command(lateSigned, new ftQueueItem { ftQueueItemId = Guid.NewGuid() }));

        var zero = TicketRequest(0m, ReceiptCase.ZeroReceipt0x2000);
        var response = (await pipeline.SignAsync(Command(zero, new ftQueueItem { ftQueueItemId = Guid.NewGuid() }))).receiptResponse;

        response.ftState.IsState(State.Error).Should().BeFalse("the SCU signed the zero receipt");
        response.ftReceiptIdentification.Should().EndWith("E1");
        response.ftState.IsFlag(StateFlags.LateSigningModeIsActive).Should().BeFalse("the zero receipt closed the mode");
    }

    /// <summary>a missing ftQueueFR row is a configuration error, never a blank row.</summary>
    [Fact]
    public async Task AMissingFtQueueFRRow_IsAConfigurationError_AndNothingIsWritten()
    {
        var empty = new InMemoryConfigurationRepository();
        var chains = new FRChainStateProvider(new AsyncLazy<IConfigurationRepository>(() => Task.FromResult<IConfigurationRepository>(empty)), Guid.NewGuid());
        var pipeline = new FRSigningPipeline(_sscd, chains);

        var sign = () => pipeline.SignAsync(Command(TicketRequest(), new ftQueueItem { ftQueueItemId = Guid.NewGuid() }));

        await sign.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ftQueueFR*");
        _sscd.Calls.Should().BeEmpty();
        (await empty.GetQueueFRAsync(Guid.Empty)).Should().BeNull();
    }

    /// <summary>
    /// A refusal is told from "signing unavailable" by the 0x001 marker of the version part alone. An
    /// error whose upper word differs otherwise (no version nibble, another country) is still a refusal.
    /// </summary>
    [Theory]
    [InlineData(0x4652_2000_EEEE_EEEEUL, true)]
    [InlineData(0x4652_0000_EEEE_EEEEUL, true)]
    [InlineData(0x0000_0000_EEEE_EEEEUL, true)]
    [InlineData(0x4652_2001_EEEE_EEEEUL, false)]
    [InlineData(0x4652_0001_EEEE_EEEEUL, false)]
    public void ARefusal_IsToldFromSigningUnavailableByItsMarkerOnly(ulong state, bool refusal)
    {
        var response = new ReceiptResponse { ftState = (State) state };

        FRSSCDErrorHandling.IsError(response).Should().BeTrue();
        FRSSCDErrorHandling.IsRefusal(response).Should().Be(refusal);
    }

    /// <summary>An SCU that answers success with no signature has not signed: the chain must not restart.</summary>
    [Fact]
    public async Task AnEmptySignature_IsSigningUnavailable_AndTheChainDoesNotMove()
    {
        var chains = NewChains();
        var pipeline = new FRSigningPipeline(_sscd, chains);
        _sscd.EmptySignature = true;

        var response = (await pipeline.SignAsync(Command(TicketRequest(), new ftQueueItem { ftQueueItemId = Guid.NewGuid() }))).receiptResponse;

        response.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        response.ftReceiptIdentification.Should().Be("ft1#");
    }

    /// <summary>An ftQueueFR store that, like a real one, hands out copies: a discarded in-memory state never leaks into it.</summary>
    private sealed class CopyingConfigurationRepository
    {
        private readonly InMemoryConfigurationRepository _store = new();
        private readonly Mock<IConfigurationRepository> _mock = new();

        public CopyingConfigurationRepository()
        {
            _mock.Setup(x => x.GetQueueFRAsync(It.IsAny<Guid>())).Returns((Guid id) => GetQueueFRAsync(id));
            _mock.Setup(x => x.InsertOrUpdateQueueFRAsync(It.IsAny<ftQueueFR>())).Returns((ftQueueFR queue) => InsertOrUpdateQueueFRAsync(queue));
        }

        public IConfigurationRepository Object => _mock.Object;

        public async Task<ftQueueFR> GetQueueFRAsync(Guid id) => Copy(await _store.GetQueueFRAsync(id));

        public Task InsertOrUpdateQueueFRAsync(ftQueueFR queue) => _store.InsertOrUpdateQueueFRAsync(Copy(queue)!);

        private static ftQueueFR? Copy(ftQueueFR? queue) => queue is null ? null : JsonSerializer.Deserialize<ftQueueFR>(JsonSerializer.Serialize(queue));
    }

    /// <summary>
    /// A forced archive whose action journal entries were lost with a failed store is recorded again
    /// when the chain state rolls forward: the limit and the closing rule give the same file.
    /// </summary>
    [Fact]
    public async Task AtStartUp_AForcedArchiveLostWithItsStore_IsRecordedAgain()
    {
        var (first, last) = SeedOpenArchiveFile();
        var crossing = await StoreSignedAsync(TicketRequest(), "ft9#T3", "stored-sig-3", row: 3, moment: new DateTime(2026, 3, 11, 9, 0, 0, DateTimeKind.Utc));

        var chains = NewChains();
        await Storage(chains).ReserveNextQueueItem(TicketRequest());

        var journals = await _actionJournals.GetByQueueItemId(crossing).ToListAsync();
        var forced = journals.Should().ContainSingle(x => x.Type.EndsWith("-ForcedArchive")).Subject;
        forced.DataJson.Should().Contain(first.ToString()).And.Contain(last.ToString());
        journals.Should().ContainSingle(x => x.Type == "30-JetPending").Which.DataJson.Should().Contain(forced.ftActionJournalId.ToString());
        (await _configuration.GetQueueFRAsync(_queueId)).MessageCount.Should().Be(1, "the JET 30 is pending again");
    }

    [Fact]
    public async Task AtStartUp_AForcedArchiveWhoseJournalsWereStored_IsNotRecordedTwice()
    {
        var (first, last) = SeedOpenArchiveFile();
        var crossing = await StoreSignedAsync(TicketRequest(), "ft9#T3", "stored-sig-3", row: 3, moment: new DateTime(2026, 3, 11, 9, 0, 0, DateTimeKind.Utc));
        var storedForced = new ftActionJournal
        {
            ftActionJournalId = Guid.NewGuid(), ftQueueId = _queueId, ftQueueItemId = crossing, Type = "4652200000000001-ForcedArchive", Moment = DateTime.UtcNow,
            DataJson = JsonSerializer.Serialize(new { First = new { ftQueueItemId = first }, Last = new { ftQueueItemId = last } }),
        };
        await _actionJournals.InsertAsync(storedForced);
        await _actionJournals.InsertAsync(new ftActionJournal
        {
            ftActionJournalId = Guid.NewGuid(), ftQueueId = _queueId, ftQueueItemId = crossing, Type = "30-JetPending", Moment = DateTime.UtcNow, Priority = -1,
            DataJson = JsonSerializer.Serialize(new { Code = 30, Message = "x", Information = $"first.ftQueueItemId={first}; last.ftQueueItemId={last}; ftActionJournalId={storedForced.ftActionJournalId}" }),
        });

        var chains = NewChains();
        await Storage(chains).ReserveNextQueueItem(TicketRequest());

        var journals = await _actionJournals.GetByQueueItemId(crossing).ToListAsync();
        journals.Should().HaveCount(2, "both entries were stored with the receipt");
        (await _configuration.GetQueueFRAsync(_queueId)).MessageCount.Should().Be(1, "the stored JET 30 is still pending, once");
    }

    /// <summary>A stored state whose open file runs from 2025-03-10 to a sale on 2025-12-01, two tickets signed.</summary>
    private (Guid first, Guid last) SeedOpenArchiveFile()
    {
        var queueFR = _configuration.GetQueueFRAsync(_queueId).GetAwaiter().GetResult();
        var file = new FROpenArchiveFile(queueFR);
        var first = Guid.NewGuid();
        var last = Guid.NewGuid();
        file.Add(TicketRequest(), new ReceiptResponse { ftQueueItemID = first, ftReceiptMoment = new DateTime(2025, 3, 10, 9, 0, 0, DateTimeKind.Utc) }, null);
        file.Add(TicketRequest(), new ReceiptResponse { ftQueueItemID = last, ftReceiptMoment = new DateTime(2025, 12, 1, 9, 0, 0, DateTimeKind.Utc) }, null);
        queueFR.TNumerator = 2;
        queueFR.TLastSignature = "stored-sig-2";
        _configuration.InsertOrUpdateQueueFRAsync(queueFR).GetAwaiter().GetResult();
        return (first, last);
    }

    private async Task<Guid> StoreSignedAsync(ReceiptRequest request, string identification, string signature, long row, DateTime? moment = null)
    {
        var at = moment ?? DateTime.UtcNow.AddMinutes(-5);
        var id = Guid.NewGuid();
        var response = new ReceiptResponse
        {
            ftQueueID = _queueId,
            ftQueueItemID = id,
            ftReceiptIdentification = identification,
            ftReceiptMoment = at,
            ftState = (State) StateFR.Success,
            ftSignatures = [new SignatureItem { ftSignatureType = (SignatureType) (long) SignatureTypeFR.ReceiptSignature, ftSignatureFormat = SignatureFormat.QRCode, Caption = "www.fiskaltrust.fr", Data = $"data.{signature}" }],
        };
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem
        {
            ftQueueItemId = id,
            ftQueueId = _queueId,
            ftQueueRow = row,
            TimeStamp = at.Ticks,
            request = JsonSerializer.Serialize(request),
            response = JsonSerializer.Serialize(response),
        });
        return id;
    }
}
