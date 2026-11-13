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
/// A half-migrated v1 queue continues in its own ftQueueFR (ADR 0003, market-fr ADR 0012), and only
/// after the daily closing of its last v1 day (market-fr#192). These tests pin what the handover
/// changes, when it refuses, and that the v2 queue then carries on from v1's state.
/// </summary>
public class V1HandoverTests
{
    private static readonly DateTime LastDailyClosing = new(2026, 11, 2, 22, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime LastSaleOfTheDay = LastDailyClosing.AddHours(-1);

    /// <summary>A v1 queue whose last day was closed: the day is rolled into the month and reset.</summary>
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
        GLastDayMoment = LastDailyClosing,
        GMonthTotalizer = 300.00m,
        GYearTotalizer = 900.00m,
        // The v1.2 archive receipt that has to precede the migration (ADR 0003 amendment 2026-11-11).
        ANumerator = 3,
        ALastQueueItemId = Guid.NewGuid(),
        ALastMoment = LastDailyClosing.AddMinutes(5),
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
    public void Apply_FoldsTheOpenMonthIntoTheYear()
    {
        var queueFR = V1QueueAtTheCut();

        FRV1Handover.Apply(queueFR, LastSaleOfTheDay).Should().BeNull();

        queueFR.GShiftTotalizer.Should().Be(15.00m);
        queueFR.GDayTotalizer.Should().Be(0m, "the last v1 day was closed");
        queueFR.GMonthTotalizer.Should().Be(300.00m, "the daily closings already rolled the days into the month");
        queueFR.GYearTotalizer.Should().Be(1200.00m, "v1 rolls the month into the year only at the monthly closing");
        queueFR.TTotalizer.Should().Be(1000.00m, "the perpetual totals are the same in both versions");
    }

    [Fact]
    public void Apply_WhileTheDayTotalsHoldTurnover_IsRefusedAndChangesNothing()
    {
        var queueFR = V1QueueAtTheCut();
        queueFR.GDayTotalizer = 40.00m;
        queueFR.GDayCITotalNormal = 40.00m;

        var refusal = FRV1Handover.Apply(queueFR, LastDailyClosing.AddHours(10), new Dictionary<FRReceiptChain, string> { [FRReceiptChain.Ticket] = "djEtc2ln" });

        refusal.Should().Contain("last day is not closed").And.Contain("40.00").And.Contain("daily closing (0x2011)");
        queueFR.GYearTotalizer.Should().Be(900.00m);
        queueFR.TLastSignature.Should().BeNull();
    }

    [Fact]
    public void Apply_AfterAReceiptCountedSinceTheLastDailyClosing_IsRefusedEvenIfTheDayNetsToZero()
    {
        // A sale and its refund leave the day totals at zero, but the day is open all the same.
        var queueFR = V1QueueAtTheCut();

        var refusal = FRV1Handover.Apply(queueFR, LastDailyClosing.AddHours(10));

        refusal.Should().Contain("last day is not closed").And.Contain("2026-11-03 08:00:00Z");
        queueFR.GYearTotalizer.Should().Be(900.00m);
    }

    [Fact]
    public void Apply_WhileTheV1LateSigningModeIsOpen_IsRefusedAndChangesNothing()
    {
        var queueFR = V1QueueAtTheCut();
        queueFR.UsedFailedCount = 2;
        queueFR.UsedFailedQueueItemId = Guid.NewGuid();
        queueFR.UsedFailedMomentMin = LastSaleOfTheDay.AddHours(-3);
        queueFR.UsedFailedMomentMax = LastSaleOfTheDay;

        var refusal = FRV1Handover.Apply(queueFR, LastSaleOfTheDay, new Dictionary<FRReceiptChain, string> { [FRReceiptChain.Ticket] = "djEtc2ln" });

        refusal.Should().Contain("late-signing mode").And.Contain("2 late-signed receipt(s)").And.Contain("zero receipt (0x2000)");
        queueFR.GYearTotalizer.Should().Be(900.00m, "a refused handover leaves ftQueueFR unchanged");
        queueFR.TLastSignature.Should().BeNull();
        queueFR.UsedFailedCount.Should().Be(2);
    }

    [Fact]
    public void Apply_WithOnlyAStaleFailedQueueItemId_IsRefused()
    {
        // v1 clears the id with the count; one without the other still means the mode was not reset.
        var queueFR = V1QueueAtTheCut();
        queueFR.UsedFailedQueueItemId = Guid.NewGuid();

        FRV1Handover.Apply(queueFR, LastSaleOfTheDay).Should().Contain("late-signing mode");
        queueFR.GYearTotalizer.Should().Be(900.00m);
    }

    [Fact]
    public void Apply_AfterTheV1ZeroReceiptClosedTheLateSigningMode_Proceeds()
    {
        var queueFR = V1QueueAtTheCut();
        queueFR.UsedFailedCount = 0;
        queueFR.UsedFailedQueueItemId = null;

        FRV1Handover.Apply(queueFR, LastSaleOfTheDay).Should().BeNull();
    }

    [Fact]
    public void Apply_ToAV1QueueThatNeverClosedADay_IsRefusedOnceItIssuedAReceipt()
    {
        var queueFR = V1QueueAtTheCut();
        queueFR.GLastDayMoment = null;

        FRV1Handover.Apply(queueFR, LastSaleOfTheDay).Should().Contain("never had a daily closing");
        FRV1Handover.Apply(queueFR, lastV1DayEntryMoment: null).Should().BeNull("a v1 queue that issued nothing has no open day");
    }

    [Fact]
    public void Apply_WithoutAV12ArchiveReceipt_IsRefusedAndChangesNothing()
    {
        var queueFR = V1QueueAtTheCut();
        queueFR.ALastQueueItemId = null;
        queueFR.ANumerator = 0;

        var refusal = FRV1Handover.Apply(queueFR, LastSaleOfTheDay);

        refusal.Should().Contain("archive receipt (0x3011)");
        queueFR.GYearTotalizer.Should().Be(900.00m);
    }

    [Fact]
    public void Apply_WhileV1MessagesArePending_IsRefusedAndChangesNothing()
    {
        var queueFR = V1QueueAtTheCut();
        queueFR.MessageCount = 2;
        queueFR.MessageMoment = LastDailyClosing.AddMinutes(-30);

        var refusal = FRV1Handover.Apply(queueFR, LastSaleOfTheDay);

        refusal.Should().Contain("2 pending message").And.Contain("zero receipt (0x2000)");
        queueFR.GYearTotalizer.Should().Be(900.00m);
    }

    [Fact]
    public void Apply_SetsTheV1SignaturesThatAreStillReadable()
    {
        var queueFR = V1QueueAtTheCut();

        FRV1Handover.Apply(queueFR, LastSaleOfTheDay, new Dictionary<FRReceiptChain, string> { [FRReceiptChain.Ticket] = "djEtc2ln" });

        queueFR.TLastSignature.Should().Be("djEtc2ln");
        queueFR.TLastHash.Should().Be("v1-ticket-hash", "the v1 hash stays for the migration event");
        queueFR.ILastSignature.Should().BeNull();
    }

    [Fact]
    public async Task AfterTheHandover_TheV2QueueContinuesTheV1ChainsAndTotals()
    {
        var queueFR = V1QueueAtTheCut();
        FRV1Handover.Apply(queueFR, LastSaleOfTheDay, new Dictionary<FRReceiptChain, string> { [FRReceiptChain.Ticket] = "djEtc2ln" });
        var sscd = new FakeFRSSCD();
        var pipeline = new FRSigningPipeline(sscd, new StoredChainState(queueFR).Provider());

        var ticket = await pipeline.SignAsync(CommandRequest(ReceiptCase.PointOfSaleReceipt0x0001));
        var copy = await pipeline.SignAsync(CommandRequest(ReceiptCase.CopyReceiptPrintExistingReceipt0x3010));
        var monthly = await pipeline.SignAsync(CommandRequest(ReceiptCase.MonthlyClosing0x2012));

        ticket.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T121");
        sscd.Calls[0].previousSignature.Should().Be("djEtc2ln");
        copy.receiptResponse.ftReceiptIdentification.Should().Be("ft1#D5", "the duplicate chain continues v1's copy chain");
        monthly.receiptResponse.ftReceiptIdentification.Should().Be("ft1#M1", "the closing chains start fresh (ADR 0002)");
        sscd.Calls[2].periodTotals!.Current.Totalizer.Should().Be(300.00m);
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
