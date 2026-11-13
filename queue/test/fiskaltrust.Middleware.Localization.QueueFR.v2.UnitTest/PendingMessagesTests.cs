using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage.Base.Helpers;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>
/// A JET event found outside a request is delivered like a v1.2 message (market-fr#200, 2026-11-07):
/// MessageIsPending on every response, the JET items on the next non-training zero receipt.
/// </summary>
public class PendingMessagesTests
{
    private readonly FakeFRSSCD _sscd = new();
    private readonly StoredChainState _state = new();
    private readonly InMemoryActionJournalRepository _actionJournals = new();
    private readonly FRSigningPipeline _pipeline;

    public PendingMessagesTests()
    {
        _pipeline = new FRSigningPipeline(_sscd, _state.Provider(), actionJournals: new AsyncLazy<IMiddlewareActionJournalRepository>(() => Task.FromResult<IMiddlewareActionJournalRepository>(_actionJournals)));
    }

    private async Task<ReceiptResponse> SignAsync(ReceiptCase receiptCase, bool training = false)
    {
        var response = await _pipeline.SignAsync(new ProcessCommandRequest(
            new ftQueue { ftQueueId = _state.QueueId },
            new ReceiptRequest
            {
                ftReceiptCase = training ? receiptCase.WithCountry("FR").WithFlag(ReceiptCaseFlags.Training) : receiptCase.WithCountry("FR"),
                Currency = Currency.EUR,
                cbReceiptReference = Guid.NewGuid().ToString("N")[..8],
                cbReceiptMoment = DateTime.UtcNow,
            },
            new ReceiptResponse
            {
                ftQueueID = _state.QueueId,
                ftQueueItemID = Guid.NewGuid(),
                ftReceiptIdentification = "ft1#",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State) StateFR.Success,
                ftSignatures = [],
            }));
        await _state.CommitAsync();
        return response.receiptResponse;
    }

    private Task RaiseAsync(int code, string label, string information)
        => _state.Provider().RaisePendingJetEventAsync(_actionJournals, code, label, information);

    private static bool Pending(ReceiptResponse response) => response.ftState.IsFlag(StateFlags.MessageIsPending);

    private static List<(string Code, string Information)> Jet(ReceiptResponse response) => response.ftSignatures
        .Where(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent))
        .Select(x => JsonDocument.Parse(x.Data).RootElement.GetProperty("FR"))
        .Select(x => (x.GetProperty("Code").GetString()!, x.GetProperty("Information").GetString()!))
        .ToList();

    [Fact]
    public async Task NothingPending_NoFlag()
    {
        var receipt = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001);

        Pending(receipt).Should().BeFalse();
    }

    [Fact]
    public async Task APendingEvent_FlagsEveryResponse_UntilAZeroReceiptReturnsIt()
    {
        await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001);
        await RaiseAsync(90, "Détection d'un défaut d'intégrité", "export; T12 request hash mismatch");
        await RaiseAsync(95, "Détection d'une rupture de la séquence continue", "export; T14 missing");

        var sale = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001);
        var closing = await SignAsync(ReceiptCase.DailyClosing0x2011);
        var zero = await SignAsync(ReceiptCase.ZeroReceipt0x2000);
        var after = await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001);

        Pending(sale).Should().BeTrue();
        Jet(sale).Should().BeEmpty("the items wait for the zero receipt");
        Pending(closing).Should().BeTrue();
        Pending(zero).Should().BeFalse();
        Jet(zero).Should().Equal(("90", "export; T12 request hash mismatch"), ("95", "export; T14 missing"));
        Pending(after).Should().BeFalse();
        Jet(after).Should().BeEmpty("each event is returned once");
        (await _state.ReadAsync()).MessageCount.Should().Be(0);
    }

    [Fact]
    public async Task ATrainingZeroReceipt_DoesNotCollect()
    {
        await RaiseAsync(220, "Restauration d'une base de données", "rollback");

        var training = await SignAsync(ReceiptCase.ZeroReceipt0x2000, training: true);
        var zero = await SignAsync(ReceiptCase.ZeroReceipt0x2000);

        Pending(training).Should().BeTrue();
        Jet(training).Should().BeEmpty();
        Jet(zero).Select(x => x.Code).Should().Equal("220");
    }

    [Fact]
    public async Task ThePendingCount_SurvivesARestart()
    {
        await RaiseAsync(90, "Détection d'un défaut d'intégrité", "export");

        var stored = await _state.ReadAsync();
        stored.MessageCount.Should().Be(1);
        stored.MessageMoment.Should().NotBeNull();
        (await _actionJournals.GetAsync()).Should().ContainSingle(x => x.Type == "90-JetPending");
    }

    [Fact]
    public async Task OrdinaryActionJournals_AreNotReturned()
    {
        await RaiseAsync(90, "Détection d'un défaut d'intégrité", "export");
        await SignAsync(ReceiptCase.PointOfSaleReceipt0x0001);
        await _actionJournals.InsertAsync(new ftActionJournal { ftActionJournalId = Guid.NewGuid(), Type = "4652000000002011-DailyClosing", Priority = -1, Moment = DateTime.UtcNow, DataJson = "{}" });

        var zero = await SignAsync(ReceiptCase.ZeroReceipt0x2000);

        Jet(zero).Select(x => x.Code).Should().Equal("90");
    }
}
