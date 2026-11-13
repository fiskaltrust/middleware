using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// The chain state moves into ftQueueFR only once the signed queue item is stored
/// (FRQueueStorageProvider): a chain must never point at an entry the queue did not keep, and a
/// restart must not lose an entry the queue did keep.
/// </summary>
public class ChainStatePersistenceTests
{
    private readonly FakeFRSSCD _sscd = new();
    private readonly FlakyConfigurationRepository _repository = new();
    private readonly Mock<IQueueStorageProvider> _inner = new();
    private readonly Guid _queueId = Guid.NewGuid();
    private readonly FRChainStateProvider _chains;
    private readonly FRSigningPipeline _pipeline;
    private readonly FRQueueStorageProvider _storage;

    public ChainStatePersistenceTests()
    {
        _repository.Seed(new ftQueueFR { ftQueueFRId = _queueId });
        _chains = new FRChainStateProvider(new AsyncLazy<IConfigurationRepository>(() => Task.FromResult(_repository.Object)), _queueId);
        _pipeline = new FRSigningPipeline(_sscd, _chains);
        _storage = new FRQueueStorageProvider(_inner.Object, _chains);
    }

    private static ProcessCommandRequest Ticket()
        => new(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest { ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"), Currency = Currency.EUR },
            new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftReceiptIdentification = "ft1#",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State) StateFR.Success,
                ftSignatures = [],
            });

    private async Task<ReceiptResponse> SignAndFinishAsync()
    {
        var response = (await _pipeline.SignAsync(Ticket())).receiptResponse;
        await _storage.FinishQueueItem(new ftQueueItem { ftQueueItemId = response.ftQueueItemID }, response);
        return response;
    }

    private Task<ftQueueFR> StoredAsync() => _repository.GetQueueFRAsync(_queueId);

    private static bool Unchanged(ftQueueFR stored) => stored.TNumerator == 0 && string.IsNullOrEmpty(stored.TLastSignature);

    [Fact]
    public async Task ASignatureAlone_StoresNothing()
    {
        await _pipeline.SignAsync(Ticket());

        Unchanged(await StoredAsync()).Should().BeTrue("the queue item that holds the signature is not stored yet");
    }

    [Fact]
    public async Task OnceTheQueueItemIsStored_TheChainStateIsStored()
    {
        await SignAndFinishAsync();

        var stored = await StoredAsync();
        stored.TNumerator.Should().Be(1);
        stored.TLastSignature.Should().Be("sig-1");
    }

    [Fact]
    public async Task WhenTheQueueItemIsNotStored_TheNextReceiptTakesTheSameNumberAndPreviousSignature()
    {
        var lost = (await _pipeline.SignAsync(Ticket())).receiptResponse;
        _inner.Setup(x => x.FinishQueueItem(It.IsAny<ftQueueItem>(), It.IsAny<ReceiptResponse>())).ThrowsAsync(new IOException("disk full"));
        var write = () => _storage.FinishQueueItem(new ftQueueItem(), lost);
        await write.Should().ThrowAsync<IOException>();

        _inner.Reset();
        var next = await SignAndFinishAsync();

        next.ftReceiptIdentification.Should().Be("ft1#T1", "T1 was never kept, so it is issued again");
        _sscd.Calls.Last().previousSignature.Should().BeNull("the chain does not cover the lost entry");
        (await StoredAsync()).TNumerator.Should().Be(1);
    }

    [Fact]
    public async Task WhenTheQueueItemIsStoredAsFailed_TheChainStateIsDropped()
    {
        var response = (await _pipeline.SignAsync(Ticket())).receiptResponse;
        response.ftState = response.ftState.WithState(State.Error);
        await _storage.FinishQueueItem(new ftQueueItem(), response);

        var next = await SignAndFinishAsync();

        next.ftReceiptIdentification.Should().Be("ft1#T1");
        (await StoredAsync()).TNumerator.Should().Be(1);
    }

    [Fact]
    public async Task WhenTheChainStateCannotBeStored_ItIsRetriedAndNothingIsSignedUntilItIs()
    {
        _repository.Fail = true;
        var first = await SignAndFinishAsync();

        first.ftReceiptIdentification.Should().Be("ft1#T1", "the receipt itself was stored and is valid");
        _inner.Verify(x => x.CreateActionJournalAsync(It.Is<string>(m => m.Contains("could not be stored")), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);

        var blocked = () => _pipeline.SignAsync(Ticket());
        await blocked.Should().ThrowAsync<InvalidOperationException>();
        _sscd.Calls.Should().HaveCount(1, "nothing is signed while T1's state is not stored");

        _repository.Fail = false;
        var second = await SignAndFinishAsync();

        second.ftReceiptIdentification.Should().Be("ft1#T2");
        _sscd.Calls.Last().previousSignature.Should().Be("sig-1");
        (await StoredAsync()).TNumerator.Should().Be(2);
    }

    [Fact]
    public async Task FrenchSignRequestsRunOneAtATime()
    {
        var running = 0;
        var maxRunning = 0;
        var inner = new Mock<fiskaltrust.Middleware.Localization.v2.Interface.ISignProcessor>();
        inner.Setup(x => x.ProcessAsync(It.IsAny<ReceiptRequest>())).Returns(async () =>
        {
            maxRunning = Math.Max(maxRunning, Interlocked.Increment(ref running));
            await Task.Delay(20);
            Interlocked.Decrement(ref running);
            return new ReceiptResponse();
        });
        var sut = new FRSignProcessor(inner.Object, _chains);

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => sut.ProcessAsync(new ReceiptRequest())));

        maxRunning.Should().Be(1);
    }

    /// <summary>An ftQueueFR store whose writes can be made to fail and that, like a real one, hands out copies.</summary>
    private sealed class FlakyConfigurationRepository
    {
        private readonly InMemoryConfigurationRepository _store = new();
        private readonly Mock<IConfigurationRepository> _mock = new();

        public FlakyConfigurationRepository()
        {
            _mock.Setup(x => x.GetQueueFRAsync(It.IsAny<Guid>())).Returns((Guid id) => GetQueueFRAsync(id));
            _mock.Setup(x => x.InsertOrUpdateQueueFRAsync(It.IsAny<ftQueueFR>()))
                .Returns((ftQueueFR queue) => Fail ? Task.FromException(new IOException("storage unavailable")) : _store.InsertOrUpdateQueueFRAsync(Copy(queue)!));
        }

        public bool Fail { get; set; }

        public IConfigurationRepository Object => _mock.Object;

        public async Task<ftQueueFR> GetQueueFRAsync(Guid id) => Copy(await _store.GetQueueFRAsync(id))!;

        public void Seed(ftQueueFR queue) => _store.InsertOrUpdateQueueFRAsync(queue).GetAwaiter().GetResult();

        private static ftQueueFR? Copy(ftQueueFR? queue) => queue is null ? null : System.Text.Json.JsonSerializer.Deserialize<ftQueueFR>(System.Text.Json.JsonSerializer.Serialize(queue));
    }
}
