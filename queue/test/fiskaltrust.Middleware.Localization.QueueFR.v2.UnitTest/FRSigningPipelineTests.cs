using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

public class FRSigningPipelineTests
{
    private static FRSigningPipeline CreatePipeline(FakeFRSSCD sscd, params ftQueueItem[] existingQueueItems)
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(existingQueueItems);
        return new FRSigningPipeline(sscd, new FRChainStateProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object))));
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
    public async Task SignAsync_PassesThePreviousHashOfTheSameChain()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(CommandRequest());
        await pipeline.SignAsync(CommandRequest(ReceiptCase.InvoiceB2B0x1002));
        await pipeline.SignAsync(CommandRequest());

        sscd.Calls[0].lastHash.Should().BeNull("the first ticket starts its chain");
        sscd.Calls[1].lastHash.Should().BeNull("the invoice chain is independent of the ticket chain");
        sscd.Calls[2].lastHash.Should().Be("hash-1", "the second ticket continues the ticket chain");
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
    public async Task SignAsync_ResumesTheChainFromTheStoredQueueItems()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd, StoredTicket(queueRow: 1, identification: "ft1#T7", chainHash: "stored-hash"));

        var response = await pipeline.SignAsync(CommandRequest());

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T8");
        sscd.Calls.Single().lastHash.Should().Be("stored-hash");
    }

    [Fact]
    public async Task SignAsync_WhenSigningFails_DoesNotConsumeAChainNumber()
    {
        var sscd = new FakeFRSSCD { ThrowOnProcess = () => new FRSigningUnavailableException() };
        var pipeline = CreatePipeline(sscd);

        var failed = await pipeline.SignAsync(CommandRequest());

        failed.receiptResponse.ftState.Should().Be((State) StateFR.SigningUnavailableError);
        failed.receiptResponse.ftReceiptIdentification.Should().Be("ft1#", "no document was issued, so no number was used");

        sscd.ThrowOnProcess = null;
        var recovered = await pipeline.SignAsync(CommandRequest());
        recovered.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T1", "NF525 requires the national numbering to be gapless");
    }

    [Fact]
    public async Task SignAsync_PropagatesFailuresThatAreNotSigningFailures()
    {
        var sscd = new FakeFRSSCD { ThrowOnProcess = () => new InvalidOperationException("boom") };
        var pipeline = CreatePipeline(sscd);

        var act = () => pipeline.SignAsync(CommandRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
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

    private static ftQueueItem StoredTicket(long queueRow, string identification, string chainHash) => new()
    {
        ftQueueItemId = Guid.NewGuid(),
        ftQueueRow = queueRow,
        request = System.Text.Json.JsonSerializer.Serialize(new ReceiptRequest { ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR") }),
        response = System.Text.Json.JsonSerializer.Serialize(new ReceiptResponse
        {
            ftState = (State) StateFR.Success,
            ftReceiptIdentification = identification,
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftReceiptMoment = DateTime.UtcNow,
            ftSignatures =
            [
                new SignatureItem
                {
                    ftSignatureType = SignatureTypeFR.ChainHash.As<SignatureType>(),
                    ftSignatureFormat = SignatureFormat.Text,
                    Caption = "Empreinte",
                    Data = chainHash,
                },
            ],
        }),
    };

    /// <summary>
    /// Named exactly like the SCU packages' exception: the queue recognizes signing failures by
    /// type name instead of referencing an SCU assembly, and this test pins that contract.
    /// </summary>
    private class FRSigningUnavailableException : Exception
    {
        public FRSigningUnavailableException() : base("signing unavailable") { }
    }
}
