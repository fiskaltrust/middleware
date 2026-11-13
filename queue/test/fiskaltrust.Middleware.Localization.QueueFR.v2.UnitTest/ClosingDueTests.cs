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
/// The closing-due flags (contract §1.4 <i>State flags</i>): DailyClosingIsDue 0x0100 once the open day is more than
/// 24 hours old, MonthlyClosingIsDue 0x0200 and YearlyClosingIsDue 0x0400 once the open month or year began in an
/// earlier calendar month or year, all on ftReceiptMoment in UTC. They are ORed with the other gggg flags
/// (ArchiveIsDue 0x0800 among them), and never block or sign.
/// </summary>
public class ClosingDueTests
{
    private const ulong Daily = 0x0100;
    private const ulong Monthly = 0x0200;
    private const ulong Yearly = 0x0400;
    private const ulong Archive = 0x0800;
    private const ulong DueMask = 0x0F00;

    private readonly FakeFRSSCD _sscd = new();
    // StoredChainState blocks on async in its constructor; built on the thread pool so it cannot starve xUnit's
    // synchronization context.
    private readonly StoredChainState _state = Task.Run(() => new StoredChainState()).GetAwaiter().GetResult();
    private readonly FRSigningPipeline _pipeline;

    public ClosingDueTests()
    {
        _pipeline = new FRSigningPipeline(_sscd, _state.Provider());
    }

    private static DateTime Utc(int year, int month, int day, int hour = 12, int minute = 0) => new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    /// <summary>Signs a receipt and returns its closing-due flags; <paramref name="withArchive"/> keeps ArchiveIsDue too.</summary>
    private async Task<ulong> SignAsync(DateTime moment, ReceiptCase receiptCase = ReceiptCase.PointOfSaleReceipt0x0001, bool training = false, bool withArchive = false)
    {
        var caseValue = receiptCase.WithCountry("FR");
        if (training)
        {
            caseValue = caseValue.WithFlag(ReceiptCaseFlags.Training);
        }

        var sale = receiptCase == ReceiptCase.PointOfSaleReceipt0x0001;
        var request = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftReceiptCase = caseValue,
            Currency = Currency.EUR,
            cbReceiptMoment = moment,
            cbChargeItems = sale ? [new ChargeItem { Amount = 10m, Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") }] : [],
            cbPayItems = sale ? [new PayItem { Amount = 10m, Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") }] : [],
        };
        var response = new ReceiptResponse
        {
            ftQueueID = _state.QueueId,
            ftQueueItemID = Guid.NewGuid(),
            ftReceiptIdentification = "ft1#",
            ftReceiptMoment = moment,
            ftState = (State) StateFR.Success,
            ftSignatures = [],
        };
        var result = await _pipeline.SignAsync(new ProcessCommandRequest(new ftQueue { ftQueueId = _state.QueueId }, request, response));
        await _state.CommitAsync();
        result.receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        return (ulong) result.receiptResponse.ftState & (withArchive ? DueMask : DueMask & ~Archive);
    }

    [Fact]
    public void TheLocalConstants_AreTheInterfaceValues()
    {
        ((ulong) StateFlagsFR.DailyClosingIsDue).Should().Be(Daily);
        ((ulong) StateFlagsFR.MonthlyClosingIsDue).Should().Be(Monthly);
        ((ulong) StateFlagsFR.YearlyClosingIsDue).Should().Be(Yearly);
    }

    [Fact]
    public async Task DailyClosingIsDue_AloneOnceTheOpenDayIsOver24Hours()
    {
        (await SignAsync(Utc(2026, 3, 10, 8))).Should().Be(0, "the open day starts here");
        (await SignAsync(Utc(2026, 3, 11, 8))).Should().Be(0, "exactly 24 hours is not over 24 hours");
        (await SignAsync(Utc(2026, 3, 11, 8, 1))).Should().Be(Daily);
    }

    [Fact]
    public async Task ADailyClosing_ClearsTheDailyFlag_AndTheNextDayStartsOnTheNextEntry()
    {
        await SignAsync(Utc(2026, 3, 10, 8));
        (await SignAsync(Utc(2026, 3, 11, 9))).Should().Be(Daily);

        (await SignAsync(Utc(2026, 3, 11, 22), ReceiptCase.DailyClosing0x2011)).Should().Be(0, "the closing ends the day, itself included");
        (await SignAsync(Utc(2026, 3, 12, 9))).Should().Be(0);
        (await SignAsync(Utc(2026, 3, 13, 8))).Should().Be(0, "the day opened at 09:00 the day before, not at the closing");
        (await SignAsync(Utc(2026, 3, 13, 9, 1))).Should().Be(Daily);
    }

    [Fact]
    public async Task MonthlyClosingIsDue_OnceTheOpenMonthBeganInAnEarlierUtcMonth()
    {
        await SignAsync(Utc(2026, 3, 31, 23, 59), ReceiptCase.DailyClosing0x2011);
        (await SignAsync(Utc(2026, 3, 31, 23, 59))).Should().Be(0);
        await SignAsync(Utc(2026, 4, 1, 0, 1), ReceiptCase.DailyClosing0x2011);
        (await SignAsync(Utc(2026, 4, 1, 0, 2))).Should().Be(Monthly, "the open month began in March (UTC); the daily closing reset only the day");
    }

    [Fact]
    public async Task AMonthlyClosing_ClearsDayAndMonth_ButNotTheYear()
    {
        await SignAsync(Utc(2025, 12, 20));
        (await SignAsync(Utc(2026, 1, 5))).Should().Be(Daily | Monthly | Yearly);

        (await SignAsync(Utc(2026, 1, 5, 13), ReceiptCase.MonthlyClosing0x2012)).Should().Be(Yearly, "a monthly closing also ends the day");
        (await SignAsync(Utc(2026, 1, 6))).Should().Be(Yearly);
        (await SignAsync(Utc(2026, 1, 6, 14), ReceiptCase.YearlyClosing0x2013)).Should().Be(0);
        (await SignAsync(Utc(2026, 1, 7))).Should().Be(0);
    }

    [Fact]
    public async Task YearlyClosingIsDue_AloneAcrossTheYear_WithTheOtherClosingsDone()
    {
        await SignAsync(Utc(2026, 12, 31, 23, 50));
        (await SignAsync(Utc(2026, 12, 31, 23, 55), ReceiptCase.MonthlyClosing0x2012)).Should().Be(0);
        (await SignAsync(Utc(2027, 1, 1, 0, 5))).Should().Be(Yearly, "the open year began on 31 December 2026");
    }

    [Fact]
    public async Task AMonthlyClosingBeforeTheMonthEnds_LeavesNoOpenMonth_UntilTheNextEntry()
    {
        await SignAsync(Utc(2026, 5, 31, 20));
        await SignAsync(Utc(2026, 5, 31, 23), ReceiptCase.MonthlyClosing0x2012);

        (await SignAsync(Utc(2026, 6, 1, 8))).Should().Be(0, "the open month starts on this June entry");
    }

    [Fact]
    public async Task TheFlagsCompose_WithArchiveIsDue()
    {
        await SignAsync(Utc(2025, 11, 20));

        (await SignAsync(Utc(2026, 1, 2), withArchive: true)).Should().Be(Daily | Monthly | Yearly | Archive, "0x0F00: each flag is ORed in, none replaces another");
        (await SignAsync(Utc(2026, 1, 2, 13), ReceiptCase.DailyClosing0x2011, withArchive: true)).Should().Be(Monthly | Yearly | Archive, "0x0E00");
    }

    [Fact]
    public async Task TrainingReceipts_NeitherOpenNorClose_ButCarryTheFlags()
    {
        (await SignAsync(Utc(2026, 3, 1), training: true)).Should().Be(0);
        (await SignAsync(Utc(2026, 4, 2), training: true)).Should().Be(0, "a training receipt opens no period");

        await SignAsync(Utc(2026, 4, 2, 13));
        (await SignAsync(Utc(2026, 4, 3, 14), ReceiptCase.DailyClosing0x2011, training: true)).Should().Be(Daily, "a training closing closes nothing");
        (await SignAsync(Utc(2026, 4, 3, 15))).Should().Be(Daily);
    }

    [Fact]
    public void Due_ComposesAllThree()
        => FRClosingDue.Due(new FRClosingDue.OpenPeriods { DayStart = Utc(2026, 1, 1), MonthStart = Utc(2026, 1, 1), YearStart = Utc(2025, 6, 1) }, Utc(2026, 2, 1))
            .Should().Be((StateFlags) (Daily | Monthly | Yearly), "0x0700");

    [Fact]
    public void Due_ReadsAMomentWithoutKindAsUtc()
        => FRClosingDue.Due(new FRClosingDue.OpenPeriods { MonthStart = new DateTime(2026, 1, 31, 23, 30, 0, DateTimeKind.Unspecified) }, Utc(2026, 1, 31, 23, 45))
            .Should().Be((StateFlags) 0);
}
