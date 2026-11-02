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

/// <summary>
/// A half-migrated v1 queue continues in its own ftQueueFR (ADR 0003, market-fr ADR 0012). These
/// tests pin what the handover changes and that the v2 queue then carries on from v1's state.
/// </summary>
public class V1HandoverTests
{
    private static ftQueueFR V1QueueAtTheCut() => new()
    {
        ftQueueFRId = Guid.NewGuid(),
        TNumerator = 120,
        TLastHash = "v1-ticket-hash",
        TTotalizer = 1000.00m,
        ITotalizer = 200.00m,
        CNumerator = 4,
        GNumerator = 30,
        GShiftTotalizer = 15.00m,
        GDayTotalizer = 40.00m,
        GMonthTotalizer = 300.00m,
        GYearTotalizer = 900.00m,
    };

    private static ProcessCommandRequest CommandRequest(ReceiptCase receiptCase)
        => new(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest { ftReceiptCase = receiptCase.WithCountry("FR"), Currency = Currency.EUR, cbChargeItems = [], cbPayItems = [] },
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
    public void Apply_FoldsTheOpenV1PeriodsIntoTheHigherOnes()
    {
        var queueFR = V1QueueAtTheCut();

        FRV1Handover.Apply(queueFR);

        queueFR.GShiftTotalizer.Should().Be(15.00m);
        queueFR.GDayTotalizer.Should().Be(40.00m);
        queueFR.GMonthTotalizer.Should().Be(340.00m, "v1 rolls the day into the month only at the daily closing");
        queueFR.GYearTotalizer.Should().Be(1240.00m, "v1 rolls the month into the year only at the monthly closing");
        queueFR.TTotalizer.Should().Be(1000.00m, "the perpetual totals are the same in both versions");
    }

    [Fact]
    public void Apply_SetsTheV1SignaturesThatAreStillReadable()
    {
        var queueFR = V1QueueAtTheCut();

        FRV1Handover.Apply(queueFR, new Dictionary<FRReceiptChain, string> { [FRReceiptChain.Ticket] = "djEtc2ln" });

        queueFR.TLastSignature.Should().Be("djEtc2ln");
        queueFR.TLastHash.Should().Be("v1-ticket-hash", "the v1 hash stays for the migration event");
        queueFR.ILastSignature.Should().BeNull();
    }

    [Fact]
    public async Task AfterTheHandover_TheV2QueueContinuesTheV1ChainsAndTotals()
    {
        var queueFR = V1QueueAtTheCut();
        FRV1Handover.Apply(queueFR, new Dictionary<FRReceiptChain, string> { [FRReceiptChain.Ticket] = "djEtc2ln" });
        var sscd = new FakeFRSSCD();
        var pipeline = new FRSigningPipeline(sscd, new StoredChainState(queueFR).Provider());

        var ticket = await pipeline.SignAsync(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001));
        var copy = await pipeline.SignAsync(CommandRequest(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010));
        var monthly = await pipeline.SignAsync(CommandRequest(ReceiptCase.MonthlyClosing0x2012));

        ticket.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T121");
        sscd.Calls[0].previousSignature.Should().Be("djEtc2ln");
        copy.receiptResponse.ftReceiptIdentification.Should().Be("ft1#D5", "the duplicate chain continues v1's copy chain");
        monthly.receiptResponse.ftReceiptIdentification.Should().Be("ft1#M1", "the closing chains start fresh (ADR 0002)");
        sscd.Calls[2].periodTotals!.Current.Totalizer.Should().Be(340.00m);
        sscd.Calls[2].periodTotals!.Perpetual.Totalizer.Should().Be(1200.00m);
    }

    [Fact]
    public async Task AQueueOpenedOnV2_StartsEveryChainAtOne()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = new FRSigningPipeline(sscd, new StoredChainState().Provider());

        var ticket = await pipeline.SignAsync(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001));

        ticket.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T1");
        sscd.Calls.Single().previousSignature.Should().BeNull();
    }
}
