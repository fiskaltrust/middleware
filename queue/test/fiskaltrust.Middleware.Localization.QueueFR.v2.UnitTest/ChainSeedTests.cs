using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
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

/// <summary>
/// A queue migrated from v1 continues the v1 chains, counters and totals (ADR 0003). These tests
/// pin the seam: the seed fills only what the v2 entries leave open.
/// </summary>
public class ChainSeedTests
{
    private class StaticSeedSource(FRChainSeed? seed) : IFRChainSeedSource
    {
        public int Reads { get; private set; }

        public Task<FRChainSeed?> GetAsync()
        {
            Reads++;
            return Task.FromResult(seed);
        }
    }

    private static FRSigningPipeline CreatePipeline(FakeFRSSCD sscd, IFRChainSeedSource? seedSource, params ftQueueItem[] existingQueueItems)
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(existingQueueItems);
        return new FRSigningPipeline(sscd, new FRChainStateProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object)), seedSource));
    }

    private static FRChainSeed Seed() => new()
    {
        Chains = new Dictionary<FRReceiptChain, FRChainSeedEntry>
        {
            [FRReceiptChain.Ticket] = new(41, "v1-ticket-signature"),
            [FRReceiptChain.DailyClosing] = new(7, null),
        },
        Totals = new Dictionary<FRTotalsPeriod, FRTotals>
        {
            [FRTotalsPeriod.Day] = new() { Totalizer = 100.00m, CINormal = 100.00m },
            [FRTotalsPeriod.Perpetual] = new() { Totalizer = 5000.00m, CINormal = 5000.00m },
        },
    };

    private static ProcessCommandRequest CommandRequest(ReceiptCase receiptCase, decimal amount = 0m)
        => new(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = receiptCase.WithCountry("FR"),
                Currency = Currency.EUR,
                cbChargeItems = amount == 0m ? [] : [new ChargeItem { Amount = amount, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }],
                cbPayItems = [],
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

    private static ftQueueItem StoredReceipt(long queueRow, ReceiptCase receiptCase, string identification, decimal amount) => new()
    {
        ftQueueItemId = Guid.NewGuid(),
        ftQueueRow = queueRow,
        request = System.Text.Json.JsonSerializer.Serialize(CommandRequest(receiptCase, amount).ReceiptRequest),
        response = System.Text.Json.JsonSerializer.Serialize(new ReceiptResponse
        {
            ftState = (State) StateFR.Success,
            ftReceiptIdentification = identification,
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftReceiptMoment = DateTime.UtcNow,
            ftSignatures = [],
        }),
    };

    [Fact]
    public async Task FirstV2Entry_ContinuesTheSeededChain()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd, new StaticSeedSource(Seed()));

        var ticket = await pipeline.SignAsync(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001, 10.00m));
        var closing = await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        ticket.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T42");
        sscd.Calls[0].chainCounter.Should().Be(42);
        sscd.Calls[0].previousSignature.Should().Be("v1-ticket-signature");

        closing.receiptResponse.ftReceiptIdentification.Should().Be("ft1#Z8");
        sscd.Calls[1].previousSignature.Should().BeNull("v1 did not keep the last signature, so the migration event carries the link");
    }

    [Fact]
    public async Task FirstV2Closing_IncludesTheSeededTotals()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd, new StaticSeedSource(Seed()));

        await pipeline.SignAsync(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001, 10.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        var totals = sscd.Calls.Last().periodTotals!;
        totals.Current.Totalizer.Should().Be(110.00m);
        totals.Perpetual.Totalizer.Should().Be(5010.00m, "a migrated queue never restarts its grand total");
    }

    [Fact]
    public async Task V2Entries_TakePrecedenceOverTheSeed()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(
            sscd,
            new StaticSeedSource(Seed()),
            StoredReceipt(1, ReceiptCase.PointOfSaleReceipt0x0001, "ft1#T42", 20.00m),
            StoredReceipt(2, ReceiptCase.DailyClosing0x2011, "ft1#Z8", 0m),
            StoredReceipt(3, ReceiptCase.PointOfSaleReceipt0x0001, "ft1#T43", 5.00m));

        var ticket = await pipeline.SignAsync(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001, 1.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        ticket.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T44");
        var totals = sscd.Calls.Last().periodTotals!;
        totals.Current.Totalizer.Should().Be(6.00m, "the v2 daily closing already closed the seeded day");
        totals.Perpetual.Totalizer.Should().Be(5026.00m, "the perpetual total spans the seed and every v2 receipt");
    }

    [Fact]
    public async Task NoSeed_LeavesAQueueOpenedOnV2Unchanged()
    {
        var sscd = new FakeFRSSCD();
        var source = new StaticSeedSource(null);
        var pipeline = CreatePipeline(sscd, source);

        var first = await pipeline.SignAsync(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001, 10.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001, 10.00m));

        first.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T1");
        sscd.Calls[0].previousSignature.Should().BeNull();
        source.Reads.Should().Be(1, "the seed is read once, when the state first loads");
    }
}
