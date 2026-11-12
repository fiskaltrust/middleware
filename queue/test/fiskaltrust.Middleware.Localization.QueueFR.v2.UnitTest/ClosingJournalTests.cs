using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// Every closing writes an action journal entry dated by the closing receipt and naming it, with the period it closed
/// and its totals; the shift closing (0x2010) writes one too.
/// </summary>
public class ClosingJournalTests
{
    private readonly StoredChainState _state = new();
    private readonly DailyOperationsCommandProcessorFR _sut;

    public ClosingJournalTests() => _sut = new DailyOperationsCommandProcessorFR(new FRSigningPipeline(new FakeFRSSCD(), _state.Provider()));

    private static ProcessCommandRequest Request(ReceiptCase receiptCase, DateTime moment) => new(
        new ftQueue { ftQueueId = Guid.NewGuid(), ftReceiptNumerator = 41, StartMoment = moment.AddDays(-3) },
        new ReceiptRequest { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = receiptCase.WithCountry("FR"), Currency = Currency.EUR, cbChargeItems = [], cbPayItems = [] },
        new ReceiptResponse
        {
            ftQueueID = Guid.NewGuid(),
            ftQueueItemID = Guid.NewGuid(),
            ftReceiptIdentification = "ft9#",
            ftReceiptMoment = moment,
            ftState = (State) StateFR.Success,
            ftSignatures = [],
        });

    public static TheoryData<ReceiptCase, string> Closings => new()
    {
        { ReceiptCase.ShiftClosing0x2010, "Shift-Closing receipt was processed." },
        { ReceiptCase.DailyClosing0x2011, "Daily-Closing receipt was processed." },
        { ReceiptCase.MonthlyClosing0x2012, "Monthly-Closing receipt was processed." },
        { ReceiptCase.YearlyClosing0x2013, "Yearly-Closing receipt was processed." },
    };

    [Theory]
    [MemberData(nameof(Closings))]
    public async Task AClosing_JournalsItselfAtItsOwnMoment(ReceiptCase receiptCase, string message)
    {
        var moment = new DateTime(2026, 11, 3, 22, 0, 0, DateTimeKind.Utc);
        var request = Request(receiptCase, moment);

        var result = receiptCase switch
        {
            ReceiptCase.ShiftClosing0x2010 => await _sut.ShiftClosing0x2010Async(request),
            ReceiptCase.DailyClosing0x2011 => await _sut.DailyClosing0x2011Async(request),
            ReceiptCase.MonthlyClosing0x2012 => await _sut.MonthlyClosing0x2012Async(request),
            _ => await _sut.YearlyClosing0x2013Async(request),
        };

        var journal = result.actionJournals.Should().ContainSingle(x => x.Message == message).Subject;
        journal.Moment.Should().Be(moment, "the journal is dated by the closing receipt, not by the clock");
        using var data = JsonDocument.Parse(journal.DataJson);
        data.RootElement.GetProperty("ftReceiptIdentification").GetString().Should().Be(result.receiptResponse.ftReceiptIdentification);
        data.RootElement.GetProperty("PeriodEnd").GetDateTime().Should().Be(moment);
        data.RootElement.TryGetProperty("Totals", out _).Should().BeTrue();
        data.RootElement.TryGetProperty("ftReceiptNumerator", out _).Should().BeFalse("the generic queue counter is not the FR chain counter");
    }
}
