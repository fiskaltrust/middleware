using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using ftJournalFRCopyPayload = fiskaltrust.Middleware.Contracts.Models.FR.ftJournalFRCopyPayload;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// A copy carries its reprint number (R13 ENC-DUP-PRN-NUM), counted per copied receipt from the
/// copies that were signed: the copy journal rows of the reference, then their queue items
/// (market-fr#235). The tests drive the processor as SignProcessor does: the queue item is stored
/// before processing and gets its response afterwards.
/// </summary>
public class DuplicateTests
{
    private readonly InMemoryJournalFRCopyPayloadRepository _copies = new();
    private readonly InMemoryQueueItemRepository _queueItems = new();
    private readonly FakeFRSSCD _sscd = new();
    private bool _failNextJournalInsert;

    private ProtocolCommandProcessorFR CreateSut()
        => new(
            new FRSigningPipeline(_sscd, new StoredChainState().Provider()),
            new AsyncLazy<IJournalFRCopyPayloadRepository>(() => Task.FromResult<IJournalFRCopyPayloadRepository>(new Journal(this))),
            new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(_queueItems)));

    /// <summary>The copy journal, which can be told to fail its next insert.</summary>
    private sealed class Journal(DuplicateTests test) : IJournalFRCopyPayloadRepository
    {
        public Task<int> GetCountOfCopiesAsync(string cbPreviousReceiptReference) => test._copies.GetCountOfCopiesAsync(cbPreviousReceiptReference);

        public Task<IEnumerable<ftJournalFRCopyPayload>> GetCopiesAsync(string copiedReceiptReference) => test._copies.GetCopiesAsync(copiedReceiptReference);

        public Task<ftJournalFRCopyPayload> GetAsync(Guid queueItemId) => test._copies.GetAsync(queueItemId);

        public Task InsertAsync(ftJournalFRCopyPayload c)
        {
            if (test._failNextJournalInsert)
            {
                test._failNextJournalInsert = false;
                throw new InvalidOperationException("storage unavailable");
            }

            return test._copies.InsertAsync(c);
        }
    }

    /// <summary>What SignProcessor does around the processor: reserve the queue item, process, finish it.</summary>
    private async Task<ProcessCommandResponse> ProcessAsync(ProtocolCommandProcessorFR sut, ProcessCommandRequest request)
    {
        var queueItem = new ftQueueItem { ftQueueItemId = request.ReceiptResponse.ftQueueItemID, ftQueueId = request.ReceiptResponse.ftQueueID, version = "v2" };
        await _queueItems.InsertOrUpdateAsync(queueItem);

        var response = await sut.CopyReceiptPrintExistingReceipt0x3010Async(request);

        queueItem.response = JsonSerializer.Serialize(response.receiptResponse);
        await _queueItems.InsertOrUpdateAsync(queueItem);
        return response;
    }

    private static ProcessCommandRequest Copy(string copiedReceiptReference)
        => new(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest
            {
                ftReceiptCase = ReceiptCase.CopyReceiptPrintExistingReceipt0x3010.WithCountry("FR"),
                Currency = Currency.EUR,
                cbPreviousReceiptReference = copiedReceiptReference,
            },
            new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftReceiptIdentification = "ft1#",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State) StateFR.Success,
                ftSignatures = [],
            });

    private static ProcessCommandRequest TrainingCopy(string copiedReceiptReference)
    {
        var request = Copy(copiedReceiptReference);
        request.ReceiptRequest.ftReceiptCase = request.ReceiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.Training);
        return request;
    }

    private static string? DuplicataOf(ProcessCommandResponse response) => response.receiptResponse.ftSignatures.SingleOrDefault(x => x.Caption == "Duplicata")?.Data;

    private int? SignedCopyNumber => FRStateData.CopyNumber(_sscd.LastRequest!.ReceiptResponse);

    [Fact]
    public async Task CopiesOfTheSameReceipt_AreNumberedOneTwoThree()
    {
        var sut = CreateSut();

        var first = await ProcessAsync(sut, Copy("R-1"));
        var second = await ProcessAsync(sut, Copy("R-1"));
        var third = await ProcessAsync(sut, Copy("R-1"));

        DuplicataOf(first).Should().Be("1. Duplicata de R-1");
        DuplicataOf(second).Should().Be("2. Duplicata de R-1");
        DuplicataOf(third).Should().Be("3. Duplicata de R-1");
        third.receiptResponse.ftReceiptIdentification.Should().Be("ft1#D3", "the duplicate chain counts every copy");
    }

    [Fact]
    public async Task ACopyOfAnotherReceipt_StartsAgainAtOne()
    {
        var sut = CreateSut();
        await ProcessAsync(sut, Copy("R-1"));
        await ProcessAsync(sut, Copy("R-1"));

        var other = await ProcessAsync(sut, Copy("R-2"));

        DuplicataOf(other).Should().Be("1. Duplicata de R-2");
    }

    [Fact]
    public async Task TheCopyNumber_ReachesTheScuInTheFRBlockOfStateData()
    {
        var sut = CreateSut();
        await ProcessAsync(sut, Copy("R-1"));

        var second = await ProcessAsync(sut, Copy("R-1"));

        SignedCopyNumber.Should().Be(2);
        MiddlewareStateData.FromReceiptResponse(second.receiptResponse).FR!.CopyNumber.Should().Be(2);
    }

    [Fact]
    public async Task ACopyIsJournaledBeforeItIsSigned_LinkingTheReferenceToItsQueueItem()
    {
        var sut = CreateSut();
        var request = Copy("R-1");

        await ProcessAsync(sut, request);

        var recorded = await _copies.GetAsync(request.ReceiptResponse.ftQueueItemID);
        recorded.CopiedReceiptReference.Should().Be("R-1");
        recorded.QueueId.Should().Be(request.ReceiptResponse.ftQueueID);
    }

    [Fact]
    public async Task ATrainingCopy_IsNeitherNumberedNorJournaled_SoTheFirstFiscalCopyIsOne()
    {
        var sut = CreateSut();

        var training = await ProcessAsync(sut, TrainingCopy("R-1"));
        var fiscal = await ProcessAsync(sut, Copy("R-1"));

        DuplicataOf(training).Should().Be("Duplicata de R-1", "a training copy carries no reprint number");
        training.receiptResponse.ftReceiptIdentification.Should().Be("ft1#X1");
        DuplicataOf(fiscal).Should().Be("1. Duplicata de R-1", "the training copy must not shift the fiscal numbering");
        SignedCopyNumber.Should().Be(1);
        (await _copies.GetCopiesAsync("R-1")).Should().ContainSingle();
    }

    [Fact]
    public async Task ACopyThatCouldNotBeSigned_IsNotCounted_AndItsRetryGetsTheNumber()
    {
        var sut = CreateSut();
        await ProcessAsync(sut, Copy("R-1"));
        _sscd.FailWith = "signing unavailable";
        var failed = await ProcessAsync(sut, Copy("R-1"));
        _sscd.FailWith = null;

        var retried = await ProcessAsync(sut, Copy("R-1"));

        failed.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        DuplicataOf(retried).Should().Be("2. Duplicata de R-1", "the failed copy did not consume number 2");
        SignedCopyNumber.Should().Be(2);
        retried.receiptResponse.ftReceiptIdentification.Should().Be("ft1#D2");
        (await _copies.GetCopiesAsync("R-1")).Should().HaveCount(3, "the failed copy keeps its journal row; it is just not counted");
    }

    [Fact]
    public async Task ACopyTheScuRefused_IsNotCounted()
    {
        var sut = CreateSut();
        _sscd.FailWith = "refused";
        _sscd.FailState = SCU.FR.Abstraction.Cases.StateFR.Error;
        await ProcessAsync(sut, Copy("R-1"));
        _sscd.FailWith = null;

        var retried = await ProcessAsync(sut, Copy("R-1"));

        DuplicataOf(retried).Should().Be("1. Duplicata de R-1");
    }

    [Fact]
    public async Task ACopyThatNeverFinished_IsNotCounted()
    {
        var sut = CreateSut();
        var interrupted = Copy("R-1");
        // The process stopped while signing: the row and the queue item exist, the response does not.
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem { ftQueueItemId = interrupted.ReceiptResponse.ftQueueItemID, version = "v2" });
        await _copies.InsertAsync(new ftJournalFRCopyPayload { QueueItemId = interrupted.ReceiptResponse.ftQueueItemID, CopiedReceiptReference = "R-1" });

        var copy = await ProcessAsync(sut, Copy("R-1"));

        DuplicataOf(copy).Should().Be("1. Duplicata de R-1");
    }

    [Fact]
    public async Task ACopyThatCannotBeJournaled_IsNotSigned()
    {
        var sut = CreateSut();
        _failNextJournalInsert = true;

        var act = () => ProcessAsync(sut, Copy("R-1"));

        // SignProcessor turns the exception into a failed response; the chain never moved.
        await act.Should().ThrowAsync<InvalidOperationException>();
        _sscd.Calls.Should().BeEmpty();
        var retried = await ProcessAsync(sut, Copy("R-1"));
        DuplicataOf(retried).Should().Be("1. Duplicata de R-1");
        retried.receiptResponse.ftReceiptIdentification.Should().Be("ft1#D1");
    }

    /// <summary>
    /// Half migration (ADR 0003): v1.2 counted every row of the reference and journaled a copy before
    /// signing it, so its failed and unfinished copies consumed numbers too. v2 continues after the
    /// highest number v1.2 printed, then filters only its own copies.
    /// </summary>
    [Fact]
    public async Task AfterAHalfMigration_TheNextCopyFollowsTheV12Count_FailedV12CopiesIncluded()
    {
        await V12Copy("R-1", response: "{\"ftState\":5067112530745229312,\"ftReceiptIdentification\":\"ft5#D1\"}");
        await V12Copy("R-1", response: "{\"ftState\":5067112530745229320,\"ftReceiptIdentification\":\"ft6#\"}");
        await V12Copy("R-1", response: null);
        await _copies.InsertAsync(new ftJournalFRCopyPayload { QueueItemId = Guid.NewGuid(), CopiedReceiptReference = "R-1" });
        var sut = CreateSut();

        var first = await ProcessAsync(sut, Copy("R-1"));
        _sscd.FailWith = "signing unavailable";
        await ProcessAsync(sut, Copy("R-1"));
        _sscd.FailWith = null;
        var second = await ProcessAsync(sut, Copy("R-1"));

        DuplicataOf(first).Should().Be("5. Duplicata de R-1", "v1.2 printed up to 4: a signed copy, a failed one, an unfinished one and a backfilled row");
        DuplicataOf(second).Should().Be("6. Duplicata de R-1", "the failed v2 copy in between does not count");
    }

    /// <summary>A copy journaled by v1.2: its queue item is not a v2 one (no <c>version</c> "v2").</summary>
    private async Task V12Copy(string copiedReceiptReference, string? response)
    {
        var queueItemId = Guid.NewGuid();
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem { ftQueueItemId = queueItemId, response = response });
        await _copies.InsertAsync(new ftJournalFRCopyPayload { QueueItemId = queueItemId, CopiedReceiptReference = copiedReceiptReference });
    }
}
