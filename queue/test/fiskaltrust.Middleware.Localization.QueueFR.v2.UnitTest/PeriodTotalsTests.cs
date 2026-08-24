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
/// A French closing receipt carries no items of its own - what it attests is the accumulated
/// turnover of the period. These tests pin that the queue computes it and hands it to the SCU.
/// </summary>
public class PeriodTotalsTests
{
    private static FRSigningPipeline CreatePipeline(FakeFRSSCD sscd, params ftQueueItem[] existingQueueItems)
    {
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(existingQueueItems);
        return new FRSigningPipeline(sscd, new FRChainStateProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object))));
    }

    private static ProcessCommandRequest Sale(decimal normal, decimal reduced1 = 0m, decimal cash = 0m)
    {
        var chargeItems = new List<ChargeItem>
        {
            new() { Amount = normal, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
        };
        if (reduced1 != 0m)
        {
            chargeItems.Add(new ChargeItem { Amount = reduced1, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.DiscountedVatRate1.WithCountry("FR") });
        }

        var request = CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001);
        request.ReceiptRequest.cbChargeItems = chargeItems;
        request.ReceiptRequest.cbPayItems =
        [
            new PayItem { Amount = cash == 0m ? normal + reduced1 : cash, Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") },
        ];
        return request;
    }

    private static ProcessCommandRequest CommandRequest(ReceiptCase receiptCase)
        => new(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = receiptCase.WithCountry("FR"), Currency = Currency.EUR, cbChargeItems = [], cbPayItems = [] },
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
    public async Task DailyClosing_SignsTheTurnoverAccumulatedSinceTheLastClosing()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 12.00m, reduced1: 5.50m));
        await pipeline.SignAsync(Sale(normal: 8.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        var totals = sscd.Calls.Last().periodTotals;
        totals.Should().NotBeNull();
        totals!.Period.Should().Be(FRTotalsPeriod.Day);
        totals.Current.Totalizer.Should().Be(25.50m);
        totals.Current.CINormal.Should().Be(20.00m);
        totals.Current.CIReduced1.Should().Be(5.50m);
        totals.Current.PICash.Should().Be(25.50m);
    }

    [Fact]
    public async Task ClosingResetsItsOwnPeriodOnly()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 10.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));
        await pipeline.SignAsync(Sale(normal: 4.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.MonthlyClosing0x2012));

        var secondDay = sscd.Calls[3].periodTotals!;
        secondDay.Current.Totalizer.Should().Be(4.00m, "the first daily closing reset the day");

        var month = sscd.Calls[4].periodTotals!;
        month.Period.Should().Be(FRTotalsPeriod.Month);
        month.Current.Totalizer.Should().Be(14.00m, "the month accumulates across the daily closings");
    }

    [Fact]
    public async Task PerpetualTotalIsReportedOnEveryClosingAndNeverReset()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 10.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));
        await pipeline.SignAsync(Sale(normal: 4.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        sscd.Calls[1].periodTotals!.Perpetual.Totalizer.Should().Be(10.00m);
        sscd.Calls[3].periodTotals!.Perpetual.Totalizer.Should().Be(14.00m);
    }

    [Fact]
    public async Task OnlySalesChainsMoveTheTotals()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 10.00m));

        var provisional = CommandRequest((ReceiptCase) 0x0006);
        provisional.ReceiptRequest.cbChargeItems = [new ChargeItem { Amount = 99.00m, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }];
        await pipeline.SignAsync(provisional);

        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        sscd.Calls.Last().periodTotals!.Current.Totalizer.Should().Be(10.00m, "a table check is not turnover");
    }

    [Fact]
    public async Task NonClosingReceipts_CarryNoPeriodTotals()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 10.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.ZeroReceipt0x2000));

        sscd.Calls[0].periodTotals.Should().BeNull();
        sscd.Calls[1].periodTotals.Should().BeNull("a zero receipt reports the chain, not a period");
    }

    [Fact]
    public async Task FailedClosing_LeavesThePeriodOpenForTheRetry()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 10.00m));

        sscd.ThrowOnProcess = () => new FRSigningUnavailableException();
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        sscd.ThrowOnProcess = null;
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        sscd.Calls.Last().periodTotals!.Current.Totalizer.Should().Be(10.00m, "the failed closing must not have reset the day");
    }

    [Fact]
    public async Task TotalsResumeFromTheStoredQueueItemsSinceTheLastClosing()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd,
            StoredReceipt(queueRow: 1, ReceiptCase.PointOfSaleReceipt0x0001, "ft1#T1", amount: 30.00m),
            StoredReceipt(queueRow: 2, ReceiptCase.DailyClosing0x2011, "ft1#G1", amount: 0m),
            StoredReceipt(queueRow: 3, ReceiptCase.PointOfSaleReceipt0x0001, "ft1#T2", amount: 7.00m));

        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        var totals = sscd.Calls.Single().periodTotals!;
        totals.Current.Totalizer.Should().Be(7.00m, "only the receipts after the stored daily closing count");
        totals.Perpetual.Totalizer.Should().Be(37.00m, "the perpetual total spans the stored closing");
    }

    private static ftQueueItem StoredReceipt(long queueRow, ReceiptCase receiptCase, string identification, decimal amount) => new()
    {
        ftQueueItemId = Guid.NewGuid(),
        ftQueueRow = queueRow,
        request = System.Text.Json.JsonSerializer.Serialize(new ReceiptRequest
        {
            ftReceiptCase = receiptCase.WithCountry("FR"),
            Currency = Currency.EUR,
            cbChargeItems = amount == 0m ? [] : [new ChargeItem { Amount = amount, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }],
            cbPayItems = [],
        }),
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

    /// <summary>Named like the SCU packages' exception - the queue recognizes it by type name.</summary>
    private class FRSigningUnavailableException : Exception
    {
        public FRSigningUnavailableException() : base("signing unavailable") { }
    }
}
