using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Models;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.v2.UnitTest.PostFiscalization;

public class IsFiscalizedTests
{
    private static readonly State _success = (State) 0x4752_2000_0000_0000;
    private static readonly State _error = ((State) 0x4752_2000_0000_0000).WithState(State.Error);

    private static ReceiptResponse Response(State state, object? stateData = null) => new()
    {
        ftQueueItemID = Guid.NewGuid(),
        cbReceiptReference = "prev",
        ftReceiptIdentification = "ft1#",
        ftState = state,
        ftStateData = stateData,
    };

    private static ReceiptResponse RoundTrip(ReceiptResponse response) => JsonSerializer.Deserialize<ReceiptResponse>(JsonSerializer.Serialize(response))!;

    private static object FinalizeFailedOutcome() => new MiddlewareStateData
    {
        PostFiscalization = new PostFiscalizationStateData { FiscalizationSucceeded = true, EInvoicing = PostFiscalizationStateData.Failed, EReporting = PostFiscalizationStateData.Disabled },
    };

    [Fact]
    public void SuccessState_IsFiscalized() => Response(_success).IsFiscalized().Should().BeTrue();

    [Fact]
    public void ErrorState_WithoutStateData_IsNotFiscalized() => Response(_error).IsFiscalized().Should().BeFalse();

    [Fact]
    public void ErrorState_WithTypedFinalizeFailedOutcome_IsFiscalized() => Response(_error, FinalizeFailedOutcome()).IsFiscalized().Should().BeTrue();

    [Fact]
    public void ErrorState_WithPersistedFinalizeFailedOutcome_IsFiscalized()
    {
        var persisted = RoundTrip(Response(_error, FinalizeFailedOutcome()));

        persisted.ftStateData.Should().BeOfType<JsonElement>();
        persisted.IsFiscalized().Should().BeTrue();
    }

    [Fact]
    public void ErrorState_WhenTheOutcomeSaysFiscalizationFailed_IsNotFiscalized()
    {
        var response = Response(_error, new MiddlewareStateData { PostFiscalization = new PostFiscalizationStateData { FiscalizationSucceeded = false } });

        response.IsFiscalized().Should().BeFalse();
        RoundTrip(response).IsFiscalized().Should().BeFalse();
    }

    [Fact]
    public void ErrorState_WithUnrelatedStateData_IsNotFiscalized()
    {
        RoundTrip(Response(_error, new { ES = new { Foo = "bar" } })).IsFiscalized().Should().BeFalse();
        Response(_error, JsonSerializer.Deserialize<JsonElement>("\"a string\"")).IsFiscalized().Should().BeFalse();
        Response(_error, "legacy string state data").IsFiscalized().Should().BeFalse();
    }

    private static Mock<IStorageProvider> StorageProviderWith(params ftQueueItem[] queueItems)
    {
        var queueItemRepository = new Mock<IMiddlewareQueueItemRepository>();
        queueItemRepository.Setup(x => x.GetByReceiptReferenceAsync("prev", It.IsAny<string?>())).Returns(queueItems.ToAsyncEnumerable());
        var storageProvider = new Mock<IStorageProvider>();
        storageProvider.Setup(x => x.CreateMiddlewareQueueItemRepository()).Returns(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(queueItemRepository.Object)));
        return storageProvider;
    }

    private static ftQueueItem FinishedQueueItem(ReceiptResponse response) => new()
    {
        ftQueueItemId = response.ftQueueItemID,
        cbReceiptReference = "prev",
        request = JsonSerializer.Serialize(new ReceiptRequest { cbReceiptReference = "prev", cbChargeItems = [], cbPayItems = [] }),
        response = JsonSerializer.Serialize(response),
        responseHash = "hash",
        ftDoneMoment = DateTime.UtcNow,
    };

    private static ReceiptRequest Referencing() => new() { cbPreviousReceiptReference = "prev", cbChargeItems = [], cbPayItems = [] };

    [Fact]
    public async Task GetReferencedReceiptsAsync_ResolvesAFinalizeFailedReceipt()
    {
        var referenced = Response(_error, FinalizeFailedOutcome());
        var provider = new QueueStorageProvider(Guid.NewGuid(), StorageProviderWith(FinishedQueueItem(referenced)).Object);

        var result = await provider.GetReferencedReceiptsAsync(Referencing());

        result.IsOk.Should().BeTrue();
        result.OkValue.Should().ContainSingle().Which.Response.ftQueueItemID.Should().Be(referenced.ftQueueItemID);
    }

    [Fact]
    public async Task GetReferencedReceiptsAsync_StillIgnoresAnUnfiscalizedErrorReceipt()
    {
        var provider = new QueueStorageProvider(Guid.NewGuid(), StorageProviderWith(FinishedQueueItem(Response(_error))).Object);

        var result = await provider.GetReferencedReceiptsAsync(Referencing());

        result.IsErr.Should().BeTrue();
        result.ErrValue.Should().Contain("didn't match");
    }

    [Fact]
    public async Task LoadOriginalReceiptWithResponseAsync_ResolvesAFinalizeFailedReceipt()
    {
        var referenced = Response(_error, FinalizeFailedOutcome());
        var storageProvider = StorageProviderWith(FinishedQueueItem(referenced));
        var provider = new ReceiptReferenceProvider(storageProvider.Object.CreateMiddlewareQueueItemRepository());

        var result = await provider.LoadOriginalReceiptWithResponseAsync("prev");

        result.Should().NotBeNull();
        result!.Value.Item2.ftQueueItemID.Should().Be(referenced.ftQueueItemID);
    }

    [Fact]
    public async Task LoadOriginalReceiptWithResponseAsync_StillIgnoresAnUnfiscalizedErrorReceipt()
    {
        var storageProvider = StorageProviderWith(FinishedQueueItem(Response(_error)));
        var provider = new ReceiptReferenceProvider(storageProvider.Object.CreateMiddlewareQueueItemRepository());

        var result = await provider.LoadOriginalReceiptWithResponseAsync("prev");

        result.Should().BeNull();
    }
}
