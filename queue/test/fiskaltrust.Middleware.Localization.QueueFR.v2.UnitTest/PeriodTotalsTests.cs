using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// A French closing receipt carries no items of its own - what it attests is the accumulated
/// turnover of the period. These tests pin that the queue computes it and hands it to the SCU.
/// </summary>
public class PeriodTotalsTests
{
    private static FRSigningPipeline CreatePipeline(FakeFRSSCD sscd, ftQueueFR? stored = null) => CreatePipeline(sscd, new StoredChainState(stored));

    private static FRSigningPipeline CreatePipeline(FakeFRSSCD sscd, StoredChainState state)
    {
        return new FRSigningPipeline(sscd, state.Provider());
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
    public async Task MonthlyClosingWithoutADailyOne_AlsoClosesTheDay()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 10.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.MonthlyClosing0x2012));
        await pipeline.SignAsync(Sale(normal: 3.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        sscd.Calls[1].periodTotals!.Current.Totalizer.Should().Be(10.00m);
        sscd.Calls[3].periodTotals!.Current.Totalizer.Should().Be(3.00m, "the monthly closing closed the day too, as in v1.2");
    }

    [Fact]
    public async Task YearlyClosing_AlsoClosesTheMonthAndTheDay_ButNotTheShift()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 10.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.YearlyClosing0x2013));
        await pipeline.SignAsync(Sale(normal: 2.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.MonthlyClosing0x2012));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.ShiftClosing0x2010));

        sscd.Calls[3].periodTotals!.Current.Totalizer.Should().Be(2.00m, "the yearly closing closed the day");
        sscd.Calls[4].periodTotals!.Current.Totalizer.Should().Be(2.00m, "the yearly closing closed the month");
        sscd.Calls[5].periodTotals!.Current.Totalizer.Should().Be(12.00m, "no closing but its own ends a shift");
    }

    [Theory]
    [InlineData(ReceiptCase.ShiftClosing0x2010, false, new[] { FRTotalsPeriod.Shift })]
    [InlineData(ReceiptCase.DailyClosing0x2011, false, new[] { FRTotalsPeriod.Day })]
    [InlineData(ReceiptCase.MonthlyClosing0x2012, false, new[] { FRTotalsPeriod.Day, FRTotalsPeriod.Month })]
    [InlineData(ReceiptCase.YearlyClosing0x2013, false, new[] { FRTotalsPeriod.Day, FRTotalsPeriod.Month, FRTotalsPeriod.Year })]
    [InlineData(ReceiptCase.InitialOperationReceipt0x4001, false, new[] { FRTotalsPeriod.Day })]
    [InlineData(ReceiptCase.OutOfOperationReceipt0x4002, false, new[] { FRTotalsPeriod.Day, FRTotalsPeriod.Month, FRTotalsPeriod.Year })]
    [InlineData(ReceiptCase.ZeroReceipt0x2000, false, new FRTotalsPeriod[0])]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, false, new FRTotalsPeriod[0])]
    [InlineData(ReceiptCase.YearlyClosing0x2013, true, new FRTotalsPeriod[0])]
    public void PeriodsClosedBy_FollowsTheV12Cascade(ReceiptCase receiptCase, bool training, FRTotalsPeriod[] expected)
    {
        var receiptCaseValue = receiptCase.WithCountry("FR");
        var request = new ReceiptRequest { ftReceiptCase = training ? receiptCaseValue.WithFlag(ReceiptCaseFlags.Training) : receiptCaseValue };

        FRPeriodTotalsAccumulator.PeriodsClosedBy(request).Should().Equal(expected);
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

    /// <summary>
    /// A sale without fiscalization obligation is a v1.2 foreign sale (v0 0x0E): signed in the
    /// bill chain, outside the grand totals.
    /// </summary>
    [Fact]
    public async Task SaleWithoutObligation_StaysOutOfTheGrandTotals()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 10.00m));
        var withoutObligation = Sale(normal: 50.00m);
        withoutObligation.ReceiptRequest.ftReceiptCase = ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003.WithCountry("FR");
        await pipeline.SignAsync(withoutObligation);
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        var totals = sscd.Calls.Last().periodTotals!;
        totals.Current.Totalizer.Should().Be(10.00m);
        totals.Perpetual.Totalizer.Should().Be(10.00m);
        withoutObligation.ReceiptResponse.ftReceiptIdentification.Should().EndWith("B1");
    }

    /// <summary>
    /// An e-commerce sale and a sale flagged as made in a foreign country are tickets and count in
    /// the grand totals: NF525 exempts neither (market-fr#221).
    /// </summary>
    [Theory]
    [InlineData(ReceiptCase.ECommerce0x0004, false)]
    [InlineData(ReceiptCase.PointOfSaleReceipt0x0001, true)]
    public async Task ECommerceAndForeignSales_CountInTheGrandTotals(ReceiptCase receiptCase, bool saleInForeignCountry)
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 10.00m));
        var sale = Sale(normal: 50.00m);
        sale.ReceiptRequest.ftReceiptCase = saleInForeignCountry
            ? receiptCase.WithCountry("FR").WithFlag(ReceiptCaseFlags.SaleInForeignCountry)
            : receiptCase.WithCountry("FR");
        await pipeline.SignAsync(sale);
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        var totals = sscd.Calls.Last().periodTotals!;
        totals.Current.Totalizer.Should().Be(60.00m);
        totals.Perpetual.Totalizer.Should().Be(60.00m);
        sale.ReceiptResponse.ftReceiptIdentification.Should().EndWith("T2");
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

        sscd.FailWith = "signing unavailable";
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        sscd.FailWith = null;
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        sscd.Calls.Last().periodTotals!.Current.Totalizer.Should().Be(10.00m, "the failed closing must not have reset the day");
    }

    [Fact]
    public async Task TrainingClosing_PrintsTheLiveTotalsAndLeavesTheRealPeriodOpen()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd);

        await pipeline.SignAsync(Sale(normal: 10.00m));
        var trainingClosing = CommandRequest(ReceiptCase.DailyClosing0x2011);
        trainingClosing.ReceiptRequest.ftReceiptCase = trainingClosing.ReceiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.Training);
        await pipeline.SignAsync(trainingClosing);

        // v1.2 (workerFR TrainingRequest): a training closing prints the live figures of the real
        // period, without resetting them (market-fr#221, decision 12).
        sscd.Calls.Last().periodTotals!.Current.Totalizer.Should().Be(10.00m, "a training closing prints the live period totals");
        trainingClosing.ReceiptResponse.ftReceiptIdentification.Should().EndWith("X1");

        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        sscd.Calls.Last().periodTotals!.Current.Totalizer.Should().Be(10.00m, "the training closing must not have reset the real day");
    }

    [Fact]
    public async Task TotalsResumeFromTheV1ColumnsOfFtQueueFR()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = CreatePipeline(sscd, new ftQueueFR { ftQueueFRId = Guid.NewGuid(), GDayTotalizer = 7.00m, TTotalizer = 30.00m, ITotalizer = 7.00m });

        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));

        var totals = sscd.Calls.Single().periodTotals!;
        totals.Current.Totalizer.Should().Be(7.00m, "the day accumulates in GDayTotalizer");
        totals.Perpetual.Totalizer.Should().Be(37.00m, "the perpetual total is the ticket plus the invoice total, as in v1");
    }

    [Fact]
    public async Task TotalsAreStoredInFtQueueFR()
    {
        var sscd = new FakeFRSSCD();
        var state = new StoredChainState();
        var pipeline = CreatePipeline(sscd, state);

        await pipeline.SignAsync(Sale(normal: 10.00m));
        await pipeline.SignAsync(CommandRequest(ReceiptCase.DailyClosing0x2011));
        await state.CommitAsync();

        var stored = await state.ReadAsync();
        stored.GDayTotalizer.Should().Be(0m, "the daily closing reset the day");
        stored.GMonthTotalizer.Should().Be(10.00m);
        stored.GMonthCITotalNormal.Should().Be(10.00m);
        stored.GMonthPITotalCash.Should().Be(10.00m);
        stored.TTotalizer.Should().Be(10.00m);
    }

    /// <summary>
    /// v1.2 keeps a totalizer per non-sales chain and the moment and queue item of the last closing
    /// of each period. v2 writes them too, next to its ADR 0012 columns (market-fr#221, decision 10).
    /// </summary>
    [Fact]
    public async Task TheV12ColumnsAreWrittenToo()
    {
        var sscd = new FakeFRSSCD();
        var state = new StoredChainState();
        var pipeline = CreatePipeline(sscd, state);

        await pipeline.SignAsync(Sale(normal: 10.00m));

        var proForma = Sale(normal: 7.00m);
        proForma.ReceiptRequest.ftReceiptCase = ((ReceiptCase) 0x0007).WithCountry("FR");
        await pipeline.SignAsync(proForma);

        var payIn = CommandRequest(ReceiptCase.PaymentTransfer0x0002);
        payIn.ReceiptRequest.cbPayItems = [new PayItem { Amount = 200.00m, Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }];
        await pipeline.SignAsync(payIn);

        var training = Sale(normal: 3.00m);
        training.ReceiptRequest.ftReceiptCase = training.ReceiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.Training);
        await pipeline.SignAsync(training);

        var monthly = CommandRequest(ReceiptCase.MonthlyClosing0x2012);
        await pipeline.SignAsync(monthly);
        await state.CommitAsync();

        var stored = await state.ReadAsync();
        stored.ATotalizer.Should().Be(10.00m, "the archive totals take the sales since the last archive");
        stored.ACITotalNormal.Should().Be(10.00m);
        stored.BTotalizer.Should().Be(7.00m);
        stored.BCITotalNormal.Should().Be(7.00m);
        stored.PTotalizer.Should().Be(0m, "a payment proof has no charge items");
        stored.PPITotalCash.Should().Be(200.00m);
        stored.XTotalizer.Should().Be(3.00m);
        stored.GLastMonthMoment.Should().Be(monthly.ReceiptResponse.ftReceiptMoment);
        stored.GLastMonthQueueItemId.Should().Be(monthly.ReceiptResponse.ftQueueItemID);
        stored.GLastDayQueueItemId.Should().Be(monthly.ReceiptResponse.ftQueueItemID, "a monthly closing also closes the day");
        stored.GLastYearQueueItemId.Should().BeNull();
    }

    private static ProcessCommandRequest DrawerMove(decimal? cashTransfer, params (PayItemCase payItemCase, decimal amount)[] payItems)
    {
        var request = CommandRequest(ReceiptCase.PaymentTransfer0x0002);
        request.ReceiptRequest.cbChargeItems = cashTransfer is { } amount
            ? [new ChargeItem { Amount = amount, Currency = Currency.EUR, VATRate = 0m, ftChargeItemCase = ((ChargeItemCase) 0x00A8).WithCountry("FR") }]
            : [];
        request.ReceiptRequest.cbPayItems = payItems.Select(x => new PayItem { Amount = x.amount, Currency = Currency.EUR, ftPayItemCase = x.payItemCase.WithCountry("FR") }).ToList();
        return request;
    }

    /// <summary>
    /// A drawer move counts in the chain P totals whichever convention it follows (market-fr ADR
    /// 0016): the cash-transfer charge item moves <c>PTotalizer</c>, and every pay item moves the
    /// bucket of its pay case, so moving money from one pay type to another moves both buckets.
    /// The perpetual total stays the sum of the pay buckets, and no sales total moves.
    /// </summary>
    [Fact]
    public async Task DrawerMoves_CountInThePaymentProofTotals_InBothConventions()
    {
        var sscd = new FakeFRSSCD();
        var state = new StoredChainState();
        var pipeline = CreatePipeline(sscd, state);

        // Reference tables: vault to drawer, then drawer to vault, as a cash transfer charge item.
        await pipeline.SignAsync(DrawerMove(500.00m, (PayItemCase.CashPayment, 500.00m)));
        await pipeline.SignAsync(DrawerMove(-120.00m, (PayItemCase.CashPayment, -120.00m)));
        // v1.2: drawer to vault as two pay items, then card money handed out as cash.
        await pipeline.SignAsync(DrawerMove(null, (PayItemCase.CashPayment, -80.00m), (PayItemCase.TransferToCashbookVaultOwnerEmployee, 80.00m)));
        await pipeline.SignAsync(DrawerMove(null, (PayItemCase.CreditCardPayment, -50.00m), (PayItemCase.CashPayment, 50.00m)));
        await state.CommitAsync();

        var stored = await state.ReadAsync();
        stored.PTotalizer.Should().Be(380.00m, "the cash transfer charge items count, the pay-items-only moves add zero");
        stored.PPITotalCash.Should().Be(350.00m);
        stored.PPITotalInternal.Should().Be(80.00m);
        stored.PPITotalNonCash.Should().Be(-50.00m);
        stored.PPITotalUnknown.Should().Be(0m);
        (stored.PPITotalCash + stored.PPITotalNonCash + stored.PPITotalInternal + stored.PPITotalUnknown).Should().Be(stored.PTotalizer);
        stored.GDayTotalizer.Should().Be(0m, "a drawer move is not a sale");
        stored.TTotalizer.Should().Be(0m);
        stored.ATotalizer.Should().Be(0m);
    }

    [Fact]
    public async Task TrainingClosing_LeavesTheV12ClosingColumnsAlone()
    {
        var sscd = new FakeFRSSCD();
        var state = new StoredChainState();
        var pipeline = CreatePipeline(sscd, state);

        var trainingClosing = CommandRequest(ReceiptCase.DailyClosing0x2011);
        trainingClosing.ReceiptRequest.ftReceiptCase = trainingClosing.ReceiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.Training);
        await pipeline.SignAsync(trainingClosing);
        await state.CommitAsync();

        (await state.ReadAsync()).GLastDayQueueItemId.Should().BeNull();
    }
}
