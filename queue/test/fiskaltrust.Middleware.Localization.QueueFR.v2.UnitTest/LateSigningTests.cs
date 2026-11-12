using fiskaltrust.Middleware.Storage.Base.Helpers;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>Late signing (market-fr#231): the range, ftState 0x8, the JET 70 item, the mention, the journal.</summary>
public class LateSigningTests
{
    private readonly FakeFRSSCD _sscd = new();
    private readonly StoredChainState _state;
    private readonly InMemoryQueueItemRepository _queueItems = new();
    private FRSigningPipeline _pipeline;

    public LateSigningTests() : this(null) { }

    private LateSigningTests(ftQueueFR? stored)
    {
        _state = new StoredChainState(stored);
        _pipeline = NewPipeline();
    }

    private FRSigningPipeline NewPipeline()
        => new(_sscd, _state.Provider(), new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(_queueItems)));

    private static readonly DateTime _t0 = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>Signs a receipt and stores it as the queue's SignProcessor would.</summary>
    private async Task<ProcessCommandResponse> SignAsync(ReceiptCase receiptCase, bool lateSigned = false, bool training = false, DateTime? cbReceiptMoment = null, string? reference = null)
    {
        var receiptCaseValue = receiptCase.WithCountry("FR");
        if (lateSigned)
        {
            receiptCaseValue = receiptCaseValue.WithFlag(ReceiptCaseFlags.LateSigning);
        }

        if (training)
        {
            receiptCaseValue = receiptCaseValue.WithFlag(ReceiptCaseFlags.Training);
        }

        var request = new ReceiptRequest
        {
            ftReceiptCase = receiptCaseValue,
            Currency = Currency.EUR,
            cbReceiptReference = reference ?? Guid.NewGuid().ToString("N")[..8],
            cbReceiptMoment = cbReceiptMoment ?? _t0,
        };
        var queueItemId = Guid.NewGuid();
        var response = await _pipeline.SignAsync(new ProcessCommandRequest(
            new ftQueue { ftQueueId = _state.QueueId },
            request,
            new ReceiptResponse
            {
                ftQueueID = _state.QueueId,
                ftQueueItemID = queueItemId,
                ftReceiptIdentification = "ft1#",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State) StateFR.Success,
                ftSignatures = [],
            }));

        await _queueItems.InsertOrUpdateAsync(new ftQueueItem
        {
            ftQueueItemId = queueItemId,
            ftQueueId = _state.QueueId,
            cbReceiptReference = request.cbReceiptReference,
            request = JsonSerializer.Serialize(request),
            response = JsonSerializer.Serialize(response.receiptResponse),
        });
        await _state.CommitAsync();
        return response;
    }

    private static bool InLateSigningMode(ProcessCommandResponse response) => response.receiptResponse.ftState.IsFlag(StateFlags.LateSigningModeIsActive);

    private static List<SignatureItem> JetItems(ProcessCommandResponse response)
        => response.receiptResponse.ftSignatures.Where(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent)).ToList();

    private static JsonElement JetData(SignatureItem item) => JsonDocument.Parse(item.Data).RootElement.GetProperty("FR");

    private static SignatureItem? Mention(ProcessCommandResponse response)
        => response.receiptResponse.ftSignatures.SingleOrDefault(x => x.Caption == "Défaillance enregistrée");

    [Fact]
    public async Task FirstLateSignedReceipt_OpensTheMode_AndAsksForJet70()
    {
        var response = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned: true, reference: "R-1");

        InLateSigningMode(response).Should().BeTrue();
        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#T1", "a late-signed receipt is signed into its normal chain");
        var jet = JetItems(response).Should().ContainSingle().Subject;
        jet.Caption.Should().Be("JET");
        ((long) jet.ftSignatureType & (long) SignatureTypeFlags.VisualizationOptional).Should().NotBe(0, "printing the JET request is optional");
        JetData(jet).GetProperty("Code").GetString().Should().Be("70");
        JetData(jet).GetProperty("Message").GetString().Should().Be("Début de mode dégradé");
        JetData(jet).GetProperty("Information").GetString().Should().Contain("cbReceiptReference=R-1").And.Contain("ftReceiptIdentification=ft1#T1");
        response.actionJournals.Should().ContainSingle(x => x.Type.EndsWith("-LateSigningStarted"));

        var stored = await _state.ReadAsync();
        stored.UsedFailedCount.Should().Be(1);
        stored.UsedFailedQueueItemId.Should().Be(response.receiptResponse.ftQueueItemID);
    }

    [Fact]
    public async Task WhileOpen_EveryResponseCarriesTheFlag_AndTheRangeWidens()
    {
        await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned: true, cbReceiptMoment: _t0.AddHours(1));
        var ordinary = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001);
        var second = await SignAsync(ReceiptCase.InvoiceB2C0x1001, lateSigned: true, cbReceiptMoment: _t0);

        InLateSigningMode(ordinary).Should().BeTrue("a receipt that is not late-signed is answered in late-signing mode too");
        JetItems(ordinary).Should().BeEmpty();
        InLateSigningMode(second).Should().BeTrue();
        JetItems(second).Should().BeEmpty("JET 70 is asked for once, when the mode opens");
        second.actionJournals.Should().BeEmpty();

        var stored = await _state.ReadAsync();
        stored.UsedFailedCount.Should().Be(2, "only late-signed receipts count");
        stored.UsedFailedMomentMin.Should().Be(_t0);
        stored.UsedFailedMomentMax.Should().Be(_t0.AddHours(1));
    }

    [Fact]
    public async Task ZeroReceipt_ClosesTheMode_WithTheMention_TheJournal_AndJet120()
    {
        await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned: true, reference: "R-1", cbReceiptMoment: _t0);
        await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned: true, reference: "R-2", cbReceiptMoment: _t0.AddMinutes(5));

        var zero = await SignAsync(ReceiptCase.ZeroReceipt0x2000, reference: "Z-1");

        InLateSigningMode(zero).Should().BeFalse("the zero receipt that ends the mode is answered outside it");
        Mention(zero)!.Data.Should().Be("du ft1#T1 au ft1#E1");
        var jet = JetItems(zero).Should().ContainSingle("the queue asks for JET 120 on the zero receipt that ends late signing (contract §6, ADR 0009)").Which;
        JetData(jet).GetProperty("Code").GetString().Should().Be("120");
        JetData(jet).GetProperty("Message").GetString().Should().Be("Fin de mode dégradé");
        JetData(jet).GetProperty("Information").GetString().Should().Contain("count=2").And.Contain("first.cbReceiptReference=R-1").And.Contain("closing.cbReceiptReference=Z-1");
        zero.actionJournals.Should().ContainSingle(x => x.Type.EndsWith("-LateSigningEnded")).Which.DataJson.Should().Contain("\"Count\":2");

        var stored = await _state.ReadAsync();
        stored.UsedFailedCount.Should().Be(0);
        stored.UsedFailedQueueItemId.Should().BeNull();
        stored.UsedFailedMomentMin.Should().BeNull();

        var after = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001);
        InLateSigningMode(after).Should().BeFalse();
    }

    [Fact]
    public async Task ZeroReceipt_OutsideTheMode_AddsNothing()
    {
        var zero = await SignAsync(ReceiptCase.ZeroReceipt0x2000);

        InLateSigningMode(zero).Should().BeFalse();
        Mention(zero).Should().BeNull();
        JetItems(zero).Should().BeEmpty();
        zero.actionJournals.Should().BeEmpty();
    }

    [Fact]
    public async Task OtherClosings_DoNotEndTheMode()
    {
        await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned: true);

        var daily = await SignAsync(ReceiptCase.DailyClosing0x2011);

        InLateSigningMode(daily).Should().BeTrue("only a zero receipt ends the mode, as in v1.2");
        Mention(daily).Should().BeNull();
    }

    [Fact]
    public async Task LateSignedZeroReceipt_OpensAndClosesTheMode()
    {
        var zero = await SignAsync(ReceiptCase.ZeroReceipt0x2000, lateSigned: true, reference: "Z-1");

        InLateSigningMode(zero).Should().BeFalse();
        JetItems(zero).Select(x => JetData(x).GetProperty("Code").GetString()).Should().Equal("70", "120");
        Mention(zero)!.Data.Should().Be("du ft1#E1 au ft1#E1");
        (await _state.ReadAsync()).UsedFailedCount.Should().Be(0);
    }

    [Fact]
    public async Task TrainingReceipts_NeitherOpenNorCloseTheMode()
    {
        var training = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned: true, training: true);
        InLateSigningMode(training).Should().BeFalse("the training chain stays apart from the fiscal chains");
        JetItems(training).Should().BeEmpty();

        await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned: true);
        var trainingZero = await SignAsync(ReceiptCase.ZeroReceipt0x2000, training: true);

        InLateSigningMode(trainingZero).Should().BeTrue("a training zero receipt does not end the mode");
        Mention(trainingZero).Should().BeNull();
        (await _state.ReadAsync()).UsedFailedCount.Should().Be(1);
    }

    [Fact]
    public async Task UnsignedLateSignedReceipt_LeavesTheModeUntouched()
    {
        _sscd.FailWith = "SCU refused";
        var refused = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned: true);

        refused.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
        JetItems(refused).Should().BeEmpty("only a signed entry opens the mode");
        refused.actionJournals.Should().BeEmpty();
        var stored = await _state.ReadAsync();
        stored.TNumerator.Should().Be(0, "nothing was signed, so nothing was stored");
        stored.UsedFailedCount.Should().Be(0);

        _sscd.FailWith = null;
        InLateSigningMode(await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001)).Should().BeFalse();
    }

    [Fact]
    public async Task UnsignedZeroReceipt_DoesNotEndTheMode()
    {
        await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned: true);
        _sscd.FailWith = "SCU refused";

        var refusedZero = await SignAsync(ReceiptCase.ZeroReceipt0x2000);

        refusedZero.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
        Mention(refusedZero).Should().BeNull();
        JetItems(refusedZero).Should().BeEmpty();
        (await _state.ReadAsync()).UsedFailedCount.Should().Be(1, "an unsigned zero receipt does not end the mode");

        _sscd.FailWith = null;
        InLateSigningMode(await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001)).Should().BeTrue();
    }

    [Fact]
    public async Task TheRangeSurvivesARestart()
    {
        var first = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001, lateSigned: true, reference: "R-1");

        _pipeline = NewPipeline();
        var afterRestart = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001);
        var zero = await SignAsync(ReceiptCase.ZeroReceipt0x2000);

        InLateSigningMode(afterRestart).Should().BeTrue("the range is read back from ftQueueFR");
        Mention(zero)!.Data.Should().Be($"du {first.receiptResponse.ftReceiptIdentification} au ft1#E1");
    }

    [Fact]
    public async Task OpenRangeFromFtQueueFR_IsContinued()
    {
        var firstQueueItemId = Guid.NewGuid();
        var queueFR = new ftQueueFR { ftQueueFRId = Guid.NewGuid(), UsedFailedCount = 3, UsedFailedQueueItemId = firstQueueItemId, UsedFailedMomentMin = _t0, UsedFailedMomentMax = _t0 };
        var test = new LateSigningTests(queueFR);

        var zero = await test.SignAsync(ReceiptCase.ZeroReceipt0x2000);

        JetItems(zero).Select(x => JetData(x).GetProperty("Code").GetString()).Should().Equal("120");
        zero.actionJournals.Should().ContainSingle(x => x.Type.EndsWith("-LateSigningEnded")).Which.DataJson.Should().Contain("\"Count\":3");
        Mention(zero)!.Data.Should().Be($"du {firstQueueItemId} au ft1#E1", "a first receipt that cannot be read back is named by its queue item id");
    }
}
