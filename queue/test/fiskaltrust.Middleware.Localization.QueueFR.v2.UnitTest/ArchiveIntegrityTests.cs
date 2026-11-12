using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
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
/// An archive receipt checks the chains of the range it signs (market-fr ADR 0009, #208): a broken link
/// asks the POS for JET 90 (integrity defect), a gap or a repeat in a chain's counters for JET 95
/// (break of the continuous sequence), as JET items on the archive receipt's own response. The receipt is still signed.
/// </summary>
public class ArchiveIntegrityTests
{
    private readonly FakeFRSSCD _sscd = new();
    private readonly InMemoryQueueItemRepository _queueItems = new();
    private readonly StoredChainState _state = new();
    private readonly FRArchiveReceipt _archive;
    private long _row;

    public ArchiveIntegrityTests()
    {
        var chains = _state.Provider();
        _archive = new FRArchiveReceipt(new FRSigningPipeline(_sscd, chains), chains,
            new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(_queueItems)),
            new AsyncLazy<IMiddlewareReceiptJournalRepository>(() => Task.FromResult<IMiddlewareReceiptJournalRepository>(new InMemoryReceiptJournalRepository())),
            new AsyncLazy<IMiddlewareActionJournalRepository>(() => Task.FromResult<IMiddlewareActionJournalRepository>(new InMemoryActionJournalRepository())));
    }

    [Fact]
    public async Task IntactChains_AskForNoIntegrityEvent()
    {
        await StoreTicketAsync(1, previous: "", signature: "sig-T1");
        await StoreTicketAsync(2, previous: "sig-T1", signature: "sig-T2");
        await StateAsync(2, "sig-T2");

        var result = await ArchiveAsync();

        JetCodes(result).Should().BeEmpty();
    }

    /// <summary>A deleted entry leaves a gap in its chain's counters.</summary>
    [Fact]
    public async Task ADeletedEntry_AsksForJet95()
    {
        await StoreTicketAsync(1, previous: "", signature: "sig-T1");
        await StoreTicketAsync(3, previous: "sig-T1", signature: "sig-T3");
        await StateAsync(3, "sig-T3");

        var result = await ArchiveAsync();

        JetCodes(result).Should().Equal("95");
        Information(result, "95").Should().Contain("chain=T").And.Contain("expected=2").And.Contain("found=3");
        result.receiptResponse.ftState.IsState(State.Error).Should().BeFalse("the defect is reported, the receipt still signed");
        result.receiptResponse.ftReceiptIdentification.Should().EndWith("A1");
        result.actionJournals.Should().NotContain(x => x.Type.EndsWith("-JetPending"), "the response itself carries the item");
    }

    /// <summary>A tampered entry no longer links to the entry before it.</summary>
    [Fact]
    public async Task ATamperedEntry_AsksForJet90()
    {
        await StoreTicketAsync(1, previous: "", signature: "sig-T1");
        await StoreTicketAsync(2, previous: "forged", signature: "sig-T2");
        await StateAsync(2, "sig-T2");

        var result = await ArchiveAsync();

        JetCodes(result).Should().Equal("90");
        Information(result, "90").Should().Contain("chain=T").And.Contain("T2");
    }

    [Fact]
    public async Task AChainStateThatDoesNotMatchItsLastStoredEntry_AsksForJet90()
    {
        await StoreTicketAsync(1, previous: "", signature: "sig-T1");
        await StateAsync(1, "another-sig");

        JetCodes(await ArchiveAsync()).Should().Equal("90");
    }

    [Fact]
    public async Task ATrainingArchiveReceipt_ChecksNothing()
    {
        await StoreTicketAsync(1, previous: "", signature: "sig-T1");
        await StoreTicketAsync(3, previous: "sig-T1", signature: "sig-T3");
        await StateAsync(3, "sig-T3");

        JetCodes(await ArchiveAsync(training: true)).Should().BeEmpty();
    }

    private async Task<ProcessCommandResponse> ArchiveAsync(bool training = false)
    {
        var caseValue = ((ReceiptCase) 0x3011).WithCountry("FR");
        var request = new ProcessCommandRequest(
            new ftQueue { ftQueueId = _state.QueueId, ftReceiptNumerator = 10 },
            new ReceiptRequest { ftCashBoxID = Guid.NewGuid(), ftReceiptCase = training ? caseValue.WithFlag(ReceiptCaseFlags.Training) : caseValue, Currency = Currency.EUR, cbChargeItems = [], cbPayItems = [] },
            new ReceiptResponse { ftQueueID = _state.QueueId, ftQueueItemID = Guid.NewGuid(), ftReceiptIdentification = "ft9#", ftReceiptMoment = DateTime.UtcNow, ftState = (State) StateFR.Success, ftSignatures = [] });
        var result = (await _archive.SignAsync(request))!;
        await _state.CommitAsync();
        return result;
    }

    /// <summary>The integrity JET codes on the response; JET 20, which every archive receipt carries, is left out.</summary>
    private static List<string> JetCodes(ProcessCommandResponse result) => AllJetCodes(result).Where(x => x != "20").ToList();

    private static IEnumerable<string> AllJetCodes(ProcessCommandResponse result) => result.receiptResponse.ftSignatures
        .Where(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent))
        .Select(x => JsonDocument.Parse(x.Data).RootElement.GetProperty("FR").GetProperty("Code").GetString()!);

    private static string Information(ProcessCommandResponse result, string code) => result.receiptResponse.ftSignatures
        .Where(x => x.ftSignatureType.IsType(SignatureTypeFR.JetEvent))
        .Select(x => JsonDocument.Parse(x.Data).RootElement.GetProperty("FR"))
        .Single(x => x.GetProperty("Code").GetString() == code)
        .GetProperty("Information").GetString()!;

    private async Task StateAsync(long ticketNumerator, string ticketSignature)
    {
        var queueFR = await _state.ReadAsync();
        queueFR.TNumerator = ticketNumerator;
        queueFR.TLastSignature = ticketSignature;
        await _state.Repository.InsertOrUpdateQueueFRAsync(queueFR);
    }

    private async Task StoreTicketAsync(long counter, string previous, string signature)
    {
        var row = ++_row;
        var moment = DateTime.UtcNow.AddMinutes(-60 + row);
        var request = new ReceiptRequest { ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001.WithCountry("FR"), cbChargeItems = [], cbPayItems = [] };
        var response = new ReceiptResponse
        {
            ftQueueItemID = Guid.NewGuid(),
            ftReceiptIdentification = $"ft{row}#T{counter}",
            ftReceiptMoment = moment,
            ftState = (State) StateFR.Success,
            ftSignatures =
            [
                new SignatureItem
                {
                    ftSignatureType = (SignatureType) (long) SignatureTypeFR.ReceiptSignature,
                    ftSignatureFormat = SignatureFormat.QRCode,
                    Caption = "www.fiskaltrust.fr",
                    Data = $"siret|CERT|{counter}|{(previous.Length == 0 ? "N" : "O")}|{previous}.{signature}",
                },
            ],
        };
        await _queueItems.InsertOrUpdateAsync(new ftQueueItem
        {
            ftQueueItemId = response.ftQueueItemID,
            ftQueueId = _state.QueueId,
            ftQueueRow = row,
            TimeStamp = moment.Ticks,
            request = JsonSerializer.Serialize(request),
            response = JsonSerializer.Serialize(response),
        });
    }
}
