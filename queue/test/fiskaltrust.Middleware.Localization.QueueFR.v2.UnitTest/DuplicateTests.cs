using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using ftJournalFRCopyPayload = fiskaltrust.Middleware.Contracts.Models.FR.ftJournalFRCopyPayload;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// A copy carries its reprint number (R13 ENC-DUP-PRN-NUM), counted per copied receipt from the
/// copy journal rather than from the queue items (market-fr#235).
/// </summary>
public class DuplicateTests
{
    private readonly InMemoryJournalFRCopyPayloadRepository _copies = new();
    private readonly FakeFRSSCD _sscd = new();

    private ProtocolCommandProcessorFR CreateSut()
        => new(new FRSigningPipeline(_sscd, new StoredChainState().Provider()), new AsyncLazy<IJournalFRCopyPayloadRepository>(() => Task.FromResult<IJournalFRCopyPayloadRepository>(_copies)));

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

    private static string? DuplicataOf(ProcessCommandResponse response) => response.receiptResponse.ftSignatures.SingleOrDefault(x => x.Caption == "Duplicata")?.Data;

    [Fact]
    public async Task CopiesOfTheSameReceipt_AreNumberedOneTwoThree()
    {
        var sut = CreateSut();

        var first = await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));
        var second = await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));
        var third = await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));

        DuplicataOf(first).Should().Be("1. Duplicata de R-1");
        DuplicataOf(second).Should().Be("2. Duplicata de R-1");
        DuplicataOf(third).Should().Be("3. Duplicata de R-1");
        third.receiptResponse.ftReceiptIdentification.Should().Be("ft1#D3", "the duplicate chain counts every copy");
    }

    [Fact]
    public async Task ACopyOfAnotherReceipt_StartsAgainAtOne()
    {
        var sut = CreateSut();
        await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));
        await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));

        var other = await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-2"));

        DuplicataOf(other).Should().Be("1. Duplicata de R-2");
    }

    [Fact]
    public async Task TheCopyNumber_IsHandedToTheScuToSign()
    {
        var sut = CreateSut();
        await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));

        await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));

        _sscd.LastRequest!.CopyNumber.Should().Be(2);
    }

    [Fact]
    public async Task EachSignedCopy_IsRecordedInTheCopyJournal()
    {
        var sut = CreateSut();

        var copy = await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));

        var recorded = await _copies.GetAsync(copy.receiptResponse.ftQueueItemID);
        recorded.CopiedReceiptReference.Should().Be("R-1");
        recorded.ReceiptId.Should().Be("ft1#D1");
    }

    [Fact]
    public async Task ACopyThatCouldNotBeSigned_IsNotCounted()
    {
        var sut = CreateSut();
        _sscd.FailWith = "signing unavailable";
        var failed = await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));
        _sscd.FailWith = null;

        var retried = await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));

        failed.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        DuplicataOf(retried).Should().Be("1. Duplicata de R-1");
    }

    [Fact]
    public async Task CopiesIssuedByV1_AreCounted()
    {
        await _copies.InsertAsync(new ftJournalFRCopyPayload { QueueItemId = Guid.NewGuid(), CopiedReceiptReference = "R-1" });
        var sut = CreateSut();

        var copy = await sut.CopyReceiptPrintExistingReceipt0x3010Async(Copy("R-1"));

        DuplicataOf(copy).Should().Be("2. Duplicata de R-1", "v1.2 writes the same copy journal, so a half-migrated queue keeps counting");
    }
}
