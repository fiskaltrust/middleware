using System.Text.Json;
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
/// The one-year cap holds on the archive file (market-fr ADR 0018): the queue flags an archive as due
/// once the open file reaches into an earlier month, and at the one-year limit it records a forced
/// archive and asks the POS for JET 30, without ever signing anything on its own.
/// </summary>
public class ArchiveDueTests
{
    private readonly FakeFRSSCD _sscd = new();
    private readonly StoredChainState _state = new();
    private readonly FRSigningPipeline _pipeline;

    public ArchiveDueTests()
    {
        _pipeline = new FRSigningPipeline(_sscd, _state.Provider());
    }

    private static DateTime Utc(int year, int month, int day, int hour = 12, int minute = 0) => new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private async Task<ProcessCommandResponse> SignAsync(DateTime moment, ReceiptCase receiptCase = ReceiptCase.PointOfSaleReceipt0x0001, bool training = false, DateTime? posMoment = null, bool lateSigned = false)
    {
        var caseValue = receiptCase.WithCountry("FR");
        if (training)
        {
            caseValue = caseValue.WithFlag(ReceiptCaseFlags.Training);
        }
        if (lateSigned)
        {
            caseValue = caseValue.WithFlag(ReceiptCaseFlags.LateSigning);
        }

        var sale = receiptCase == ReceiptCase.PointOfSaleReceipt0x0001;
        var request = new ReceiptRequest
        {
            ftCashBoxID = Guid.NewGuid(),
            ftReceiptCase = caseValue,
            Currency = Currency.EUR,
            cbReceiptMoment = posMoment ?? moment,
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
        return result;
    }

    private static bool Due(ProcessCommandResponse result) => ((ulong) result.receiptResponse.ftState & 0x0800) == 0x0800;

    private static List<ftActionJournal> Forced(ProcessCommandResponse result) => result.actionJournals.Where(x => x.Type?.EndsWith("-ForcedArchive") == true).ToList();

    private static (Guid first, Guid last) Bounds(ftActionJournal forced)
    {
        using var data = JsonDocument.Parse(forced.DataJson);
        return (data.RootElement.GetProperty("First").GetProperty("ftQueueItemId").GetGuid(), data.RootElement.GetProperty("Last").GetProperty("ftQueueItemId").GetGuid());
    }

    private static List<int> PendingJetCodes(ProcessCommandResponse result) => result.actionJournals
        .Where(x => x.Type?.EndsWith("-JetPending") == true)
        .Select(x => JsonDocument.Parse(x.DataJson).RootElement.GetProperty("Code").GetInt32())
        .ToList();

    private static List<string> JetCodes(ProcessCommandResponse result) => result.receiptResponse.ftSignatures
        .Where(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent))
        .Select(x => JsonDocument.Parse(x.Data).RootElement.GetProperty("FR").GetProperty("Code").GetString()!)
        .ToList();

    [Fact]
    public async Task ArchiveIsDue_OnceTheOpenFileReachesIntoAnEarlierUtcMonth()
    {
        Due(await SignAsync(Utc(2026, 1, 31, 23, 58))).Should().BeFalse("the open file starts this month");
        Due(await SignAsync(Utc(2026, 1, 31, 23, 59))).Should().BeFalse();
        Due(await SignAsync(Utc(2026, 2, 1, 0, 1))).Should().BeTrue("the open file's first record is in January");
    }

    [Fact]
    public async Task AnArchiveReceipt_ClearsTheFlag_AndTheNextFileStartsAfterIt()
    {
        await SignAsync(Utc(2026, 1, 10));
        Due(await SignAsync(Utc(2026, 2, 3))).Should().BeTrue();

        Due(await SignAsync(Utc(2026, 2, 3, 13), (ReceiptCase) 0x3011)).Should().BeFalse("the archive receipt closes the open file, itself included");
        Due(await SignAsync(Utc(2026, 3, 1))).Should().BeFalse("the next file starts on this receipt");
        Due(await SignAsync(Utc(2026, 4, 1))).Should().BeTrue();
    }

    [Fact]
    public async Task ATrainingArchiveReceipt_ClearsNothing()
    {
        await SignAsync(Utc(2026, 1, 10));

        Due(await SignAsync(Utc(2026, 2, 3), (ReceiptCase) 0x3011, training: true)).Should().BeTrue();
        Due(await SignAsync(Utc(2026, 2, 4))).Should().BeTrue();
    }

    [Fact]
    public async Task AnArchiveReceipt_AsksForJet20_ATrainingOneDoesNot()
    {
        await SignAsync(Utc(2026, 1, 10));

        JetCodes(await SignAsync(Utc(2026, 1, 11), (ReceiptCase) 0x3011)).Should().Equal("20");
        JetCodes(await SignAsync(Utc(2026, 1, 12), (ReceiptCase) 0x3011, training: true)).Should().BeEmpty();
    }

    [Fact]
    public async Task ACertificateChange_StartsANewOpenFile()
    {
        _sscd.CertificateSerialNumber = "CERT-A";
        await SignAsync(Utc(2025, 1, 10));
        await SignAsync(Utc(2025, 6, 10));

        _sscd.CertificateSerialNumber = "CERT-B";
        var first = await SignAsync(Utc(2026, 3, 1));

        Due(first).Should().BeFalse("the new certificate's first entry opens the file");
        Forced(first).Should().BeEmpty("a forced file never spans a certificate change");
        Due(await SignAsync(Utc(2026, 4, 1))).Should().BeTrue();
    }

    [Fact]
    public async Task AtTheLimit_TheForcedFileEndsOnTheLastDailyClosingBeforeIt()
    {
        var first = await SignAsync(Utc(2025, 3, 10, 9));
        var closing = await SignAsync(Utc(2026, 2, 1, 22), ReceiptCase.DailyClosing0x2011);
        await SignAsync(Utc(2026, 2, 5));

        var justBefore = await SignAsync(Utc(2026, 3, 9, 23, 59));
        Forced(justBefore).Should().BeEmpty("the limit is 2026-03-10 00:00 UTC");

        var crossing = await SignAsync(Utc(2026, 3, 10, 0, 1));

        var forced = Forced(crossing).Should().ContainSingle().Subject;
        Bounds(forced).Should().Be((first.receiptResponse.ftQueueItemID, closing.receiptResponse.ftQueueItemID));
        PendingJetCodes(crossing).Should().Equal(30);
        crossing.receiptResponse.ftState.IsFlag(StateFlags.MessageIsPending).Should().BeTrue();
        Due(crossing).Should().BeTrue("the next file starts on the sale after the closing, in February");
        crossing.receiptResponse.ftReceiptIdentification.Should().StartWith("ft1#T", "the queue signs nothing on its own");
    }

    [Fact]
    public async Task AtTheLimit_WithoutADailyClosing_TheForcedFileEndsOnTheEntryBeforeTheNewOne()
    {
        var first = await SignAsync(Utc(2025, 3, 10, 9));
        var last = await SignAsync(Utc(2025, 12, 1));

        var crossing = await SignAsync(Utc(2026, 3, 10, 0, 1));

        Bounds(Forced(crossing).Should().ContainSingle().Subject).Should().Be((first.receiptResponse.ftQueueItemID, last.receiptResponse.ftQueueItemID));
        Due(crossing).Should().BeFalse("the next file starts on the new entry");
    }

    [Fact]
    public async Task AfterAnIdleGapOfYears_EachFileStaysWithinItsYear()
    {
        var first = await SignAsync(Utc(2024, 1, 15, 9));
        var closing = await SignAsync(Utc(2024, 1, 15, 23), ReceiptCase.DailyClosing0x2011);
        var after = await SignAsync(Utc(2024, 1, 16));

        var back = await SignAsync(Utc(2026, 6, 1));

        Forced(back).Select(Bounds).Should().Equal(
            (first.receiptResponse.ftQueueItemID, closing.receiptResponse.ftQueueItemID),
            (after.receiptResponse.ftQueueItemID, after.receiptResponse.ftQueueItemID));
        PendingJetCodes(back).Should().Equal(30, 30);
        Due(back).Should().BeFalse();
    }

    [Fact]
    public async Task ALateSignedReceipt_IsPlacedByTheQueueClock()
    {
        await SignAsync(Utc(2026, 1, 10));

        var late = await SignAsync(Utc(2026, 1, 20), lateSigned: true, posMoment: Utc(2025, 12, 1));

        Due(late).Should().BeFalse("the queue clock says January, whatever the POS moment");

        var first = await SignAsync(Utc(2026, 1, 21));
        var crossing = await SignAsync(Utc(2027, 1, 10, 0, 1), lateSigned: true, posMoment: Utc(2026, 1, 25));
        Forced(crossing).Should().ContainSingle("the queue clock puts it past the limit, whatever the POS moment");
    }

    [Fact]
    public async Task TheOpenFileSurvivesARestart()
    {
        await SignAsync(Utc(2026, 1, 10));

        var restarted = new FRSigningPipeline(_sscd, new StoredChainState(await _state.ReadAsync()).Provider());
        var response = await restarted.SignAsync(new ProcessCommandRequest(
            new ftQueue { ftQueueId = _state.QueueId },
            new ReceiptRequest { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = ReceiptCase.ZeroReceipt0x2000.WithCountry("FR"), Currency = Currency.EUR, cbChargeItems = [], cbPayItems = [] },
            new ReceiptResponse { ftQueueID = _state.QueueId, ftQueueItemID = Guid.NewGuid(), ftReceiptIdentification = "ft1#", ftReceiptMoment = Utc(2026, 2, 1), ftState = (State) StateFR.Success, ftSignatures = [] }));

        Due(response).Should().BeTrue("the open file's start is stored in ftQueueFR");
    }
}
