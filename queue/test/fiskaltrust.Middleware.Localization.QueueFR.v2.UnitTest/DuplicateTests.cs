using fiskaltrust.Middleware.Storage.Base.Helpers;
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
using Moq;
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
            new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(QueueItemsReadOnlyByIdAndRow())));

    /// <summary>
    /// The queue items as the copy processor may read them: by ID and queue row, the read that is a
    /// point read in every storage, and by receipt reference, which every storage indexes (the JET
    /// 155/156 item looks up the copied receipt's case, market-fr#200). Any other read - by ID alone
    /// scans in Azure - fails the test.
    /// </summary>
    private IMiddlewareQueueItemRepository QueueItemsReadOnlyByIdAndRow()
    {
        var queueItems = new Mock<IMiddlewareQueueItemRepository>(MockBehavior.Strict);
        queueItems.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<long>())).Returns((Guid id, long row) => _queueItems.GetAsync(id, row));
        queueItems.Setup(x => x.GetByReceiptReferenceAsync(It.IsAny<string>(), It.IsAny<string>())).Returns((string reference, string terminal) => _queueItems.GetByReceiptReferenceAsync(reference, terminal));
        // Only a copy of a pre-transaction document reads on from its original, to find what finalised or voided it (§6.3.3.6-7).
        queueItems.Setup(x => x.GetEntriesOnOrAfterTimeStampAsync(It.IsAny<long>(), It.IsAny<int?>())).Returns((long from, int? take) =>
        {
            ReadOnFromOriginal = true;
            return _queueItems.GetEntriesOnOrAfterTimeStampAsync(from, take);
        });
        return queueItems.Object;
    }

    private static long _lastQueueRow;

    private bool ReadOnFromOriginal { get; set; }

    /// <summary>A signed receipt stored as SignProcessor leaves it.</summary>
    private async Task StoreSignedAsync(ReceiptCase receiptCase, string reference, string identification, string? previousReference = null)
    {
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem
        {
            ftQueueItemId = Guid.NewGuid(),
            ftQueueRow = Interlocked.Increment(ref _lastQueueRow),
            cbReceiptReference = reference,
            version = "v2",
            request = JsonSerializer.Serialize(new ReceiptRequest
            {
                ftReceiptCase = (ReceiptCase) ((long) receiptCase | 0x4652_2000_0000_0000),
                cbReceiptReference = reference,
                cbPreviousReceiptReference = previousReference,
                Currency = Currency.EUR,
            }),
            response = JsonSerializer.Serialize(new ReceiptResponse { ftState = (State) StateFR.Success, ftReceiptIdentification = identification, ftSignatures = [] }),
        });
    }

    private JsonElement SignedStateFR() => JsonSerializer.SerializeToElement(_sscd.LastRequest!.ReceiptResponse.ftStateData).GetProperty("FR");

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
        var queueItem = new ftQueueItem { ftQueueItemId = request.ReceiptResponse.ftQueueItemID, ftQueueRow = request.ReceiptResponse.ftQueueRow, ftQueueId = request.ReceiptResponse.ftQueueID, version = "v2" };
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
                ftQueueRow = Interlocked.Increment(ref _lastQueueRow),
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

    [Theory]
    [InlineData(ReceiptCase.Order0x3004, "155")]
    [InlineData(ReceiptCase.Pay0x3005, "156")]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, null)]
    public async Task ACopy_AsksForTheJetEventOfTheCopiedReceipt(ReceiptCase copiedCase, string? code)
    {
        var sut = CreateSut();
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem
        {
            ftQueueItemId = Guid.NewGuid(),
            ftQueueRow = Interlocked.Increment(ref _lastQueueRow),
            cbReceiptReference = "B-1",
            version = "v2",
            request = JsonSerializer.Serialize(new ReceiptRequest { ftReceiptCase = copiedCase.WithCountry("FR"), cbReceiptReference = "B-1", Currency = Currency.EUR }),
            response = JsonSerializer.Serialize(new ReceiptResponse { ftState = (State) StateFR.Success, ftReceiptIdentification = "ft1#B1", ftSignatures = [] }),
        });

        var copy = await ProcessAsync(sut, Copy("B-1"));

        var jet = copy.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent)).ToList();
        if (code is null)
        {
            jet.Should().BeEmpty("R19 has no code for a copy of a ticket");
        }
        else
        {
            jet.Should().ContainSingle().Which.Data.Should().Contain($"\"Code\":\"{code}\"").And.Contain("copied.cbReceiptReference=B-1");
        }
    }

    /// <summary>R13 ENC-DUP-TYP and ENC-DUP-ORI-NUM: the copy says what it copies, from the copied receipt itself.</summary>
    [Theory]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, "ft1#T7", "ticket")]
    [InlineData((ReceiptCase) 0x0006, "ft1#B2", "note")]
    [InlineData(ReceiptCase.PaymentTransfer0x0002, "ft1#P3", "payment-proof")]
    [InlineData(ReceiptCase.InvoiceB2C0x1001, "ft1#I4", "invoice")]
    public async Task ACopy_StoresTheTypeAndTheFiscalNumberOfTheCopiedReceipt(ReceiptCase copiedCase, string identification, string copyType)
    {
        var sut = CreateSut();
        await StoreSignedAsync(copiedCase, "O-1", identification);

        var copy = await ProcessAsync(sut, Copy("O-1"));

        copy.receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        SignedStateFR().GetProperty("CopyType").GetString().Should().Be(copyType);
        SignedStateFR().GetProperty("CopiedReceiptIdentification").GetString().Should().Be(identification);
    }

    [Fact]
    public async Task ACopyOfAReceiptNotStoredHere_IsSigned_WithoutAType()
    {
        var copy = await ProcessAsync(CreateSut(), Copy("V1-42"));

        copy.receiptResponse.ftState.IsState(State.Error).Should().BeFalse("a v1.2 receipt answered elsewhere can still be copied");
        SignedStateFR().TryGetProperty("CopyType", out _).Should().BeFalse();
    }

    /// <summary>Contract §6: 156 on a copy of any payment proof, the transfer receipt included.</summary>
    [Fact]
    public async Task ACopyOfAPaymentTransfer_AsksForJet156()
    {
        await StoreSignedAsync(ReceiptCase.PaymentTransfer0x0002, "P-1", "ft1#P1");

        var copy = await ProcessAsync(CreateSut(), Copy("P-1"));

        copy.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent)).Should().ContainSingle()
            .Which.Data.Should().Contain("\"Code\":\"156\"");
    }

    /// <summary>NF525 V2.3 §6.3.3.6: no duplicate of a pre-transaction document once a sale finalised it.</summary>
    [Theory]
    [InlineData((ReceiptCase) 0x0006)]
    [InlineData((ReceiptCase) 0x0007)]
    [InlineData(ReceiptCase.Order0x3004)]
    public async Task ACopyOfAFinalisedPreTransactionDocument_IsRefused(ReceiptCase copiedCase)
    {
        await StoreSignedAsync(copiedCase, "N-1", "ft1#B1");
        await StoreSignedAsync(ReceiptCase.PointOfSaleReceipt0x0001, "T-1", "ft1#T1", previousReference: "N-1");
        var calls = _sscd.Calls.Count;

        var copy = await ProcessAsync(CreateSut(), Copy("N-1"));

        copy.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
        JsonSerializer.Serialize(copy.receiptResponse.ftSignatures).Should().Contain("T-1");
        _sscd.Calls.Count.Should().Be(calls, "nothing is signed");
        (await _copies.GetCopiesAsync("N-1")).Should().BeEmpty("a refused copy takes no number");
    }

    /// <summary>NF525 V2.3 §6.3.3.7: no duplicate of an abandoned document; a voided pre-transaction document is one.</summary>
    [Fact]
    public async Task ACopyOfAVoidedNote_IsRefused()
    {
        await StoreSignedAsync((ReceiptCase) 0x0006, "N-1", "ft1#B1");
        await StoreSignedAsync((ReceiptCase) 0x0004_0006, "N-1-VOID", "ft1#B2", previousReference: "N-1");

        var copy = await ProcessAsync(CreateSut(), Copy("N-1"));

        copy.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
    }

    [Fact]
    public async Task ACopyOfAnOpenNote_IsSigned()
    {
        await StoreSignedAsync((ReceiptCase) 0x0006, "N-1", "ft1#B1");
        await StoreSignedAsync(ReceiptCase.PointOfSaleReceipt0x0001, "T-1", "ft1#T1", previousReference: "N-9");

        var copy = await ProcessAsync(CreateSut(), Copy("N-1"));

        copy.receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        ReadOnFromOriginal.Should().BeTrue();
    }

    [Fact]
    public async Task ACopyOfATicket_ReadsNothingButItsOriginal()
    {
        await StoreSignedAsync(ReceiptCase.PointOfSaleReceipt0x0001, "T-1", "ft1#T1");

        await ProcessAsync(CreateSut(), Copy("T-1"));

        ReadOnFromOriginal.Should().BeFalse("only a pre-transaction document can be finalised or abandoned");
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
        recorded.QueueRow.Should().Be(request.ReceiptResponse.ftQueueRow, "the queue row makes the outcome a point read");
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
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem { ftQueueItemId = interrupted.ReceiptResponse.ftQueueItemID, ftQueueRow = interrupted.ReceiptResponse.ftQueueRow, version = "v2" });
        await _copies.InsertAsync(new ftJournalFRCopyPayload { QueueItemId = interrupted.ReceiptResponse.ftQueueItemID, QueueRow = interrupted.ReceiptResponse.ftQueueRow, CopiedReceiptReference = "R-1" });

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
    /// signing it, so a copy that failed after being journaled consumed its number too. v2 continues after the
    /// highest number v1.2 printed, then filters only its own copies.
    /// </summary>
    [Fact]
    public async Task AfterAHalfMigration_TheNextCopyFollowsTheV12Count_UnfinishedV12CopiesIncluded()
    {
        await V12Copy("R-1", response: "{\"ftState\":5067112530745229312,\"ftReceiptIdentification\":\"ft5#D1\"}");
        // Late-signed: v1.2 ORs 0x8 into ftState for a receipt carrying the late-signing flag.
        await V12Copy("R-1", response: "{\"ftState\":5067112530745229320,\"ftReceiptIdentification\":\"ft6#D2\"}");
        // Journaled, then never answered: v1.2 still counted it.
        await V12Copy("R-1", response: null);
        await _copies.InsertAsync(new ftJournalFRCopyPayload { QueueItemId = Guid.NewGuid(), CopiedReceiptReference = "R-1" });
        var sut = CreateSut();

        var first = await ProcessAsync(sut, Copy("R-1"));
        _sscd.FailWith = "signing unavailable";
        await ProcessAsync(sut, Copy("R-1"));
        _sscd.FailWith = null;
        var second = await ProcessAsync(sut, Copy("R-1"));

        DuplicataOf(first).Should().Be("5. Duplicata de R-1", "v1.2 printed up to 4: a signed copy, a late-signed one, an unfinished one and a backfilled row");
        DuplicataOf(second).Should().Be("6. Duplicata de R-1", "the failed v2 copy in between does not count");
    }

    /// <summary>A copy journaled by v1.2: its row has no queue row, which only v2 writes.</summary>
    private async Task V12Copy(string copiedReceiptReference, string? response)
    {
        var queueItemId = Guid.NewGuid();
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem { ftQueueItemId = queueItemId, response = response });
        await _copies.InsertAsync(new ftJournalFRCopyPayload { QueueItemId = queueItemId, CopiedReceiptReference = copiedReceiptReference });
    }
}
