using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// Every receipt of the closing chains S, Z, M, Y and of the technical chain E stores which period it closes
/// (contract §1.3: <c>ClosingType</c>, <c>PeriodStart</c>, <c>PeriodEnd</c>) in <c>ftStateData.FR.Closing</c>.
/// A period starts where the previous closing of that period ended, or at the queue's start; it ends on the
/// closing receipt. <c>Periods</c> lists every period the receipt closes with its start, which the LNE and
/// self-certified SCUs sign in data-set field 20 (archive-v2 §5).
/// </summary>
public class ClosingPeriodTests
{
    private static readonly DateTime QueueStart = new(2026, 11, 1, 6, 0, 0, DateTimeKind.Utc);

    private readonly FakeFRSSCD _sscd = new();
    private readonly StoredChainState _state = new();
    private readonly FRSigningPipeline _pipeline;

    public ClosingPeriodTests() => _pipeline = new FRSigningPipeline(_sscd, _state.Provider());

    [Fact]
    public async Task TheFirstDailyClosing_ClosesTheDaySinceTheQueueStarted()
    {
        var closing = await SignAsync(ReceiptCase.DailyClosing0x2011, At(1, 22));

        Closing(closing).Should().Be(("daily", QueueStart, At(1, 22)));
    }

    [Fact]
    public async Task ADailyClosing_ClosesTheDaySinceThePreviousOne()
    {
        await SignAsync(ReceiptCase.DailyClosing0x2011, At(1, 22));

        var closing = await SignAsync(ReceiptCase.DailyClosing0x2011, At(2, 22));

        Closing(closing).Should().Be(("daily", At(1, 22), At(2, 22)));
    }

    [Fact]
    public async Task AMonthlyClosing_ClosesTheMonth_NotTheDaysItAlsoEnds()
    {
        await SignAsync(ReceiptCase.DailyClosing0x2011, At(1, 22));
        await SignAsync(ReceiptCase.MonthlyClosing0x2012, At(30, 22));

        var nextDay = await SignAsync(ReceiptCase.DailyClosing0x2011, At(30, 23));
        var nextMonth = await SignAsync(ReceiptCase.MonthlyClosing0x2012, At(30, 23, 30));

        Closing(nextDay).Should().Be(("daily", At(30, 22), At(30, 23)), "the monthly closing also closed the day");
        Closing(nextMonth).Should().Be(("monthly", At(30, 22), At(30, 23, 30)));
    }

    [Theory]
    [InlineData(ReceiptCase.ShiftClosing0x2010, "shift")]
    [InlineData(ReceiptCase.YearlyClosing0x2013, "yearly")]
    public async Task ShiftAndYearlyClosings_NameTheirType(ReceiptCase receiptCase, string closingType)
        => Closing(await SignAsync(receiptCase, At(3, 12))).Should().Be((closingType, QueueStart, At(3, 12)));

    [Fact]
    public async Task AnOutOfOperationReceipt_ClosesTheYear()
    {
        await SignAsync(ReceiptCase.YearlyClosing0x2013, At(2, 12));

        Closing(await SignAsync(ReceiptCase.OutOfOperationReceipt0x4002, At(5, 12))).Should().Be(("lifecycle", At(2, 12), At(5, 12)));
    }

    [Fact]
    public async Task AnInitialOperationReceipt_ClosesTheDayItOpens()
        => Closing(await SignAsync(ReceiptCase.InitialOperationReceipt0x4001, At(1, 6), queueStart: null)).Should().Be(("lifecycle", At(1, 6), At(1, 6)));

    [Fact]
    public async Task AZeroReceipt_ClosesNoPeriod()
    {
        var zero = await SignAsync(ReceiptCase.ZeroReceipt0x2000, At(1, 12));

        ClosingType(zero).Should().Be("zero");
        Property(zero, "PeriodStart").Should().BeNull();
        Property(zero, "PeriodEnd").Should().BeNull();
    }

    [Fact]
    public async Task ASaleOrATrainingClosing_CarriesNoClosingPeriod()
    {
        (await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, At(1, 12))).ftStateData.Should().NotBeNull();
        Block(await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, At(1, 12))).Should().BeNull();
        Block(await SignAsync(ReceiptCase.DailyClosing0x2011, At(1, 22), training: true)).Should().BeNull();
    }

    [Fact]
    public async Task TheClosingPeriod_IsNotHandedToTheScu()
    {
        await SignAsync(ReceiptCase.DailyClosing0x2011, At(1, 22));

        JsonSerializer.Serialize(_sscd.Calls.Last().periodTotals).Should().NotContain("PeriodStart");
    }

    [Fact]
    public async Task AMonthlyClosing_ListsTheDayAndTheMonth_WithTheirStarts()
    {
        await SignAsync(ReceiptCase.DailyClosing0x2011, At(1, 22));

        var monthly = await SignAsync(ReceiptCase.MonthlyClosing0x2012, At(30, 22));

        FRStateData.ClosedPeriods(monthly).Select(x => (x.Period, x.Start)).Should().Equal(
            (FRTotalsPeriod.Day, At(1, 22)), (FRTotalsPeriod.Month, QueueStart));
    }

    [Fact]
    public async Task TheQueueStop_ListsTheDayTheMonthAndTheYear()
    {
        await SignAsync(ReceiptCase.YearlyClosing0x2013, At(2, 12));

        var stop = await SignAsync(ReceiptCase.OutOfOperationReceipt0x4002, At(5, 12));

        FRStateData.ClosedPeriods(stop).Select(x => (x.Period, x.Start)).Should().Equal(
            (FRTotalsPeriod.Day, At(2, 12)), (FRTotalsPeriod.Month, At(2, 12)), (FRTotalsPeriod.Year, At(2, 12)));
        FRSignedRecords.Field20(Request(ReceiptCase.OutOfOperationReceipt0x4002), stop).Should().Be(
            $"Day,{FRSignedRecords.Moment(At(2, 12))};Month,{FRSignedRecords.Moment(At(2, 12))};Year,{FRSignedRecords.Moment(At(2, 12))}");
    }

    [Theory]
    [InlineData(ReceiptCase.ShiftClosing0x2010, FRTotalsPeriod.Shift)]
    [InlineData(ReceiptCase.DailyClosing0x2011, FRTotalsPeriod.Day)]
    public async Task AShiftOrDailyClosing_ListsOnlyItsOwnPeriod(ReceiptCase receiptCase, FRTotalsPeriod period)
        => FRStateData.ClosedPeriods(await SignAsync(receiptCase, At(3, 12))).Select(x => (x.Period, x.Start)).Should().Equal((period, QueueStart));

    [Fact]
    public async Task AZeroReceiptOrATrainingClosing_ListsNoPeriod()
    {
        FRStateData.ClosedPeriods(await SignAsync(ReceiptCase.ZeroReceipt0x2000, At(1, 12))).Should().BeEmpty();
        FRStateData.ClosedPeriods(await SignAsync(ReceiptCase.DailyClosing0x2011, At(1, 22), training: true)).Should().BeEmpty();
    }

    private static ReceiptRequest Request(ReceiptCase receiptCase) => new() { ftReceiptCase = receiptCase.WithCountry("FR") };

    private static DateTime At(
int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0, DateTimeKind.Utc);

    private async Task<ReceiptResponse> SignAsync(ReceiptCase receiptCase, DateTime moment, bool training = false, DateTime? queueStart = default)
    {
        var caseValue = receiptCase.WithCountry("FR");
        var result = await _pipeline.SignAsync(new ProcessCommandRequest(
            new ftQueue { ftQueueId = _state.QueueId, StartMoment = queueStart ?? (receiptCase == ReceiptCase.InitialOperationReceipt0x4001 ? null : QueueStart) },
            new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = training ? caseValue.WithFlag(ReceiptCaseFlags.Training) : caseValue,
                Currency = Currency.EUR,
                cbReceiptMoment = moment,
                cbChargeItems = [],
                cbPayItems = [],
            },
            new ReceiptResponse
            {
                ftQueueID = _state.QueueId,
                ftQueueItemID = Guid.NewGuid(),
                ftReceiptIdentification = "ft1#",
                ftReceiptMoment = moment,
                ftState = (State) StateFR.Success,
                ftSignatures = [],
            }));
        await _state.CommitAsync();
        return result.receiptResponse;
    }

    private static JsonElement? Block(ReceiptResponse response)
    {
        if (response.ftStateData is null)
        {
            return null;
        }
        var state = JsonSerializer.SerializeToElement(response.ftStateData);
        return state.TryGetProperty("FR", out var fr) && fr.TryGetProperty("Closing", out var closing) ? closing : null;
    }

    private static string? Property(ReceiptResponse response, string name)
        => Block(response) is { } block && block.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? ClosingType(ReceiptResponse response) => Property(response, "ClosingType");

    private static (string?, DateTime?, DateTime?) Closing(ReceiptResponse response)
        => (ClosingType(response),
            Property(response, "PeriodStart") is { } start ? DateTime.Parse(start, null, System.Globalization.DateTimeStyles.RoundtripKind) : null,
            Property(response, "PeriodEnd") is { } end ? DateTime.Parse(end, null, System.Globalization.DateTimeStyles.RoundtripKind) : null);
}
