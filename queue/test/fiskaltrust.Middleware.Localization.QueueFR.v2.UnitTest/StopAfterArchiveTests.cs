using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// A queue is taken out of operation (0x4002) only right after a non-training archive receipt, as v1.2 archived then
/// stopped: every record is then in a signed archive range before the queue closes (C11; Ref LNE 1.8 Ex. 7, 10; [08 G6]).
/// </summary>
public class StopAfterArchiveTests
{
    private readonly FakeFRSSCD _sscd = new();
    private readonly StoredChainState _state = new();
    private readonly InMemoryQueueItemRepository _queueItems = new();
    private readonly Mock<ILocalizedQueueStorageProvider> _storage = new();
    private readonly LifecycleCommandProcessorFR _sut;
    private long _row;

    public StopAfterArchiveTests()
    {
        var chains = _state.Provider();
        _sut = new LifecycleCommandProcessorFR(_sscd, new FRSigningPipeline(_sscd, chains), _storage.Object, chains: chains,
            queueItems: new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(_queueItems)));
    }

    [Fact]
    public async Task AStopRightAfterAnArchiveReceipt_TakesTheQueueOutOfOperation()
    {
        await StoreAsync("T", 1);
        await ArchiveAsync();

        var response = await StopAsync();

        response.receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        _storage.Verify(x => x.DeactivateQueueAsync(), Times.Once);
    }

    [Fact]
    public async Task AStopWithoutAnyArchiveReceipt_IsRefused()
    {
        await StoreAsync("T", 1);

        await RefusedAsync("archive receipt");
    }

    [Fact]
    public async Task AStopAfterASaleThatFollowedTheArchiveReceipt_IsRefused()
    {
        await ArchiveAsync();
        await StoreAsync("T", 2);

        await RefusedAsync("ft2#T2");
    }

    /// <summary>A compliant POS sends the JET 20 its archive receipt asked for (0x3001, chain L) before it stops.</summary>
    [Fact]
    public async Task AStopAfterTheArchiveReceiptsJetEvent_TakesTheQueueOutOfOperation()
    {
        await ArchiveAsync();
        await StoreAsync("L", 1, 0x3001);

        (await StopAsync()).receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        _storage.Verify(x => x.DeactivateQueueAsync(), Times.Once);
    }

    /// <summary>Training documents add nothing to an archive, so they do not keep the queue running.</summary>
    [Fact]
    public async Task AStopAfterATrainingDocument_TakesTheQueueOutOfOperation()
    {
        await ArchiveAsync();
        await StoreAsync("X", 1);

        (await StopAsync()).receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
    }

    [Fact]
    public async Task AStopAfterAJetEventAndThenASale_IsRefused()
    {
        await ArchiveAsync();
        await StoreAsync("L", 1, 0x3001);
        await StoreAsync("T", 2);

        await RefusedAsync("ft3#T2");
    }

    [Fact]
    public async Task AnUnsignedItemAfterTheArchiveReceipt_DoesNotBlockTheStop()
    {
        await ArchiveAsync();
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem { ftQueueItemId = Guid.NewGuid(), ftQueueId = _state.QueueId, ftQueueRow = ++_row, request = "{}", response = null });

        (await StopAsync()).receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
    }

    private async Task RefusedAsync(string reason)
    {
        var response = await StopAsync();

        response.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
        JsonSerializer.Serialize(response.receiptResponse.ftSignatures).Should().Contain(reason);
        _storage.Verify(x => x.DeactivateQueueAsync(), Times.Never);
        _sscd.Calls.Should().BeEmpty("nothing is signed");
    }

    private Task<ProcessCommandResponse> StopAsync() => _sut.OutOfOperationReceipt0x4002Async(new ProcessCommandRequest(
        new ftQueue { ftQueueId = _state.QueueId, ftReceiptNumerator = 10 },
        new ReceiptRequest { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = ReceiptCase.OutOfOperationReceipt0x4002.WithCountry("FR"), Currency = Currency.EUR, cbChargeItems = [], cbPayItems = [] },
        new ReceiptResponse { ftQueueID = _state.QueueId, ftQueueItemID = Guid.NewGuid(), ftReceiptIdentification = "ft9#", ftReceiptMoment = DateTime.UtcNow, ftState = (State) StateFR.Success, ftSignatures = [] }));

    /// <summary>A signed archive receipt the chain state points at.</summary>
    private async Task ArchiveAsync()
    {
        var id = await StoreAsync("A", 1, 0x3011);
        var queueFR = await _state.ReadAsync();
        queueFR.ALastQueueItemId = id;
        queueFR.ANumerator = 1;
        await _state.Repository.InsertOrUpdateQueueFRAsync(queueFR);
    }

    private async Task<Guid> StoreAsync(string chain, long counter, long receiptCase = 0x0001)
    {
        var row = ++_row;
        var id = Guid.NewGuid();
        var training = chain == "X";
        var request = new ReceiptRequest
        {
            ftReceiptCase = (ReceiptCase) (0x4652_2000_0000_0000 | receiptCase | (training ? 0x0002_0000L : 0)),
            cbChargeItems = [],
            cbPayItems = [],
        };
        var response = new ReceiptResponse
        {
            ftQueueItemID = id,
            ftReceiptIdentification = $"ft{row}#{chain}{counter}",
            ftReceiptMoment = DateTime.UtcNow,
            ftState = (State) StateFR.Success,
            ftSignatures =
            [
                new SignatureItem
                {
                    ftSignatureType = (SignatureType) (long) SignatureTypeFR.ReceiptSignature,
                    ftSignatureFormat = SignatureFormat.QRCode,
                    Caption = "www.fiskaltrust.fr",
                    Data = $"siret|CERT|{counter}|N|.sig-{chain}{counter}",
                },
            ],
        };
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem
        {
            ftQueueItemId = id,
            ftQueueId = _state.QueueId,
            ftQueueRow = row,
            request = JsonSerializer.Serialize(request),
            response = JsonSerializer.Serialize(response),
        });
        return id;
    }
}
