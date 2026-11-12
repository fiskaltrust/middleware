using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

public class FRSigningPipelineTests
{
    private static FRSigningPipeline CreatePipeline(FakeFRSSCD sscd, ftQueueFR? stored = null) => CreatePipeline(sscd, new StoredChainState(stored));

    private static FRSigningPipeline CreatePipeline(FakeFRSSCD sscd, StoredChainState state)
    {
        return new FRSigningPipeline(sscd, state.Provider());
    }

    private static ProcessCommandRequest CommandRequest(ReceiptCase receiptCase = ReceiptCase.PointOfSaleReceipt0x0001)
        => new(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest { ftReceiptCase = receiptCase.WithCountry("FR"), Currency = Currency.EUR },
            new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftReceiptIdentification = "ft1#",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State) StateFR.Success,
                ftSignatures = [],
            });

    [Fact]
    public async Task SignAsync_NumbersEachChainIndependently()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        var firstTicket = await pipeline.SignAsync(CommandRequest());
        var secondTicket = await pipeline.SignAsync(CommandRequest());
        var invoice = await pipeline.SignAsync(CommandRequest(ReceiptCase.InvoiceB2B0x1002));

        firstTicket.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T1");
        secondTicket.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T2");
        invoice.receiptResponse.ftReceiptIdentification.Should().Be("ft1#I1");
    }

    [Fact]
    public async Task SignAsync_PassesThePreviousSignatureOfTheSameChain()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(CommandRequest());
        await pipeline.SignAsync(CommandRequest(ReceiptCase.InvoiceB2B0x1002));
        await pipeline.SignAsync(CommandRequest());

        sscd.Calls[0].previousSignature.Should().BeNull("the first ticket starts its chain");
        sscd.Calls[1].previousSignature.Should().BeNull("the invoice chain is independent of the ticket chain");
        sscd.Calls[2].previousSignature.Should().Be("sig-1", "the second ticket continues the ticket chain");
    }

    [Fact]
    public async Task SignAsync_ChainsEachClosingTypeOnItsOwnPredecessor()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        var firstDay = await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));
        var zero = await pipeline.SignAsync(CommandRequest(ReceiptCase.ZeroReceipt0x2000));
        var month = await pipeline.SignAsync(CommandRequest(ReceiptCase.MonthlyClosing0x2012));
        var secondDay = await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        firstDay.receiptResponse.ftReceiptIdentification.Should().Be("ft1#Z1");
        zero.receiptResponse.ftReceiptIdentification.Should().Be("ft1#E1", "a zero receipt is not a closing (ADR 0002)");
        month.receiptResponse.ftReceiptIdentification.Should().Be("ft1#M1");
        secondDay.receiptResponse.ftReceiptIdentification.Should().Be("ft1#Z2");

        sscd.Calls[1].previousSignature.Should().BeNull("the technical chain is independent of the closing chains");
        sscd.Calls[2].previousSignature.Should().BeNull("the monthly closing does not chain on a daily closing");
        sscd.Calls[3].previousSignature.Should().Be("sig-1", "a daily closing chains on the previous daily closing");
    }

    [Fact]
    public async Task SignAsync_HandsTheChainCounterToTheScu()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(CommandRequest());
        await pipeline.SignAsync(CommandRequest(ReceiptCase.InvoiceB2B0x1002));
        await pipeline.SignAsync(CommandRequest());

        sscd.Calls.Select(x => x.chainCounter).Should().Equal([1L, 1L, 2L], "the counter is the entry's position in its own chain");
    }

    [Fact]
    public async Task SignAsync_HandsTheFinalReceiptIdentificationToTheScu()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(CommandRequest());

        sscd.Calls.Single().receiptIdentification.Should().Be("ft1#T1", "the number is part of the signed data set");
    }

    [Fact]
    public async Task SignAsync_ResumesTheChainFromFtQueueFR()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd, new ftQueueFR { ftQueueFRId = Guid.NewGuid(), TNumerator = 7, TLastSignature = "c3RvcmVkLXNpZw" });

        var response = await pipeline.SignAsync(CommandRequest());

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T8");
        sscd.Calls.Single().previousSignature.Should().Be("c3RvcmVkLXNpZw");
        sscd.Calls.Single().chainCounter.Should().Be(8);
    }

    [Fact]
    public async Task SignAsync_ContinuesTheCounterOfAV1ChainWithoutASignature()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd, new ftQueueFR { ftQueueFRId = Guid.NewGuid(), TNumerator = 41, TLastHash = "v1-hash", CNumerator = 3 });

        var ticket = await pipeline.SignAsync(CommandRequest());
        var copy = await pipeline.SignAsync(CommandRequest(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010));

        ticket.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T42", "a half-migrated queue keeps counting in the v1 column");
        copy.receiptResponse.ftReceiptIdentification.Should().Be("ft1#D4", "the duplicate chain continues the v1 copy chain");
        sscd.Calls.First().previousSignature.Should().BeNull("v1 stores a hash, never the signature the v2 chain covers");
    }

    [Fact]
    public async Task SignAsync_StoresTheAdvancedChainInFtQueueFR()
    {
        var sscd = new FakeFRSSCD();
        var state = new StoredChainState();
        var pipeline = CreatePipeline(sscd, state);

        await pipeline.SignAsync(CommandRequest());
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));
        await state.CommitAsync();

        var stored = await state.ReadAsync();
        stored.TNumerator.Should().Be(1);
        stored.TLastSignature.Should().Be("sig-1");
        stored.ZNumerator.Should().Be(1, "the daily closing chain has a column of its own");
        stored.ZLastSignature.Should().Be("sig-2");
        stored.GNumerator.Should().Be(0, "the v1 grand-total chain is not continued (ADR 0002)");
    }

    [Fact]
    public async Task SignAsync_WhenSigningFails_StoresNothing()
    {
        var sscd = new FakeFRSSCD { FailWith = "signing unavailable" };
        var state = new StoredChainState();

        await CreatePipeline(sscd, state).SignAsync(CommandRequest());
        await state.CommitAsync();

        (await state.ReadAsync()).TNumerator.Should().Be(0, "no entry was signed, so no chain moved");
    }

    [Fact]
    public async Task SignAsync_WhenSigningFails_DoesNotConsumeAChainNumber()
    {
        var sscd = new FakeFRSSCD { FailWith = "private key unavailable" };
        var pipeline = CreatePipeline(sscd);

        var failed = await pipeline.SignAsync(CommandRequest());

        failed.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        failed.receiptResponse.ftReceiptIdentification.Should().Be("ft1#", "no document was issued, so no number was used");
        failed.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE")
            .Which.Data.Should().Contain("private key unavailable", "the SCU's reason reaches the POS");

        sscd.FailWith = null;
        var recovered = await pipeline.SignAsync(CommandRequest());
        recovered.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T1", "NF525 requires the national numbering to be gapless");
        sscd.Calls.Last().previousSignature.Should().BeNull("the failed call returned no signature to chain on");
    }

    [Fact]
    public async Task SignAsync_ReadsTheFailureFromTheScusResponse_NotFromItsOwnObject()
    {
        // An SCU behind REST (signing.fiskaltrust.fr) answers with a copy of the receipt response.
        var sscd = new FakeFRSSCD { FailWith = "private key unavailable", FailsOverJson = true };
        var pipeline = CreatePipeline(sscd);

        var failed = await pipeline.SignAsync(CommandRequest());

        failed.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        failed.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
        failed.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE")
            .Which.Data.Should().Contain("private key unavailable");
    }

    [Fact]
    public async Task SignAsync_WhenTheScuRefusesTheReceipt_ReportsARefusalWithoutConsumingANumber()
    {
        var sscd = new FakeFRSSCD { FailWith = "unknown event code", FailState = SCU.FR.Abstraction.Cases.StateFR.Error };
        var pipeline = CreatePipeline(sscd);

        var refused = await pipeline.SignAsync(CommandRequest());

        refused.receiptResponse.ftState.Should().Be((State) StateFR.Error, "a refusal is not an unavailable signing unit");
        refused.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
        refused.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE")
            .Which.Data.Should().Contain("refused").And.Contain("unknown event code");
    }

    public static TheoryData<Exception> CallFailures => new()
    {
        new HttpRequestException("connection refused"),
        new TaskCanceledException("timeout"),
        new InvalidOperationException("boom"),
    };

    [Theory]
    [MemberData(nameof(CallFailures))]
    public async Task SignAsync_WhenTheScuCallThrows_ReportsTheSameErrorWithoutConsumingANumber(Exception failure)
    {
        // An SCU behind REST may not answer at all; whatever the exception, the receipt is unsigned.
        var sscd = new FakeFRSSCD { ThrowOnProcess = () => failure };
        var pipeline = CreatePipeline(sscd);

        var failed = await pipeline.SignAsync(CommandRequest());

        failed.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        failed.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
        failed.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE")
            .Which.Data.Should().Contain(failure.Message);

        sscd.ThrowOnProcess = null;
        var recovered = await pipeline.SignAsync(CommandRequest());
        recovered.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T1");
    }

    [Fact]
    public async Task SignAsync_KeepsTheActionJournalsOfTheCaller()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);
        var actionJournal = new ftActionJournal { ftActionJournalId = Guid.NewGuid() };

        var response = await pipeline.SignAsync(CommandRequest(), [actionJournal]);

        response.actionJournals.Should().ContainSingle().Which.Should().Be(actionJournal);
    }
}
