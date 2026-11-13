using fiskaltrust.Middleware.Storage.Base.Helpers;
using Moq;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Validation;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.FR;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Contracts.Repositories.FR;
using fiskaltrust.Middleware.Contracts.Repositories;
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
/// Technical events of the NF525 JET (Infocert R19 v4.2), market-fr#200, seen from the queue: the
/// queue refuses a malformed event before any SCU sees it (one behaviour for every body), and acts
/// on the SCU's response otherwise.
/// </summary>
public class JetEventTests
{
    private static (ProtocolCommandProcessorFR sut, FakeFRSSCD sscd) CreateSut()
    {
        var sscd = new FakeFRSSCD();
        var pipeline = new FRSigningPipeline(sscd, new StoredChainState().Provider());
        return (new ProtocolCommandProcessorFR(pipeline, new AsyncLazy<IJournalFRCopyPayloadRepository>(() => Task.FromResult<IJournalFRCopyPayloadRepository>(new InMemoryJournalFRCopyPayloadRepository())), new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult<IMiddlewareQueueItemRepository>(new InMemoryQueueItemRepository()))), sscd);
    }

    private static ProcessCommandRequest TechnicalEvent(object? caseData, object? cbUser = null)
        => new(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = ReceiptCase.ProtocolTechnicalEvent0x3001.WithCountry("FR"),
                ftReceiptCaseData = caseData,
                cbUser = cbUser,
                Currency = Currency.EUR,
            },
            new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftReceiptIdentification = "ft1#",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = (State) StateFR.Success,
                ftSignatures = [],
            });

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public async Task ValidTechnicalEvent_IsSignedIntoTheLogChain()
    {
        var (sut, sscd) = CreateSut();

        var response = await sut.ProtocolTechnicalEvent0x3001Async(TechnicalEvent(Json("{\"FR\":{\"Code\":\"90\",\"Message\":\"Archivage\",\"Information\":\"Z12\"}}"), "Alice"));

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#L1");
        sscd.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task TechnicalEventTheScuRefuses_TakesNoNumberOfTheLogChain()
    {
        var (sut, sscd) = CreateSut();
        sscd.FailWith = "The technical event cannot enter the NF525 technical event journal: no code";
        sscd.FailState = SCU.FR.Abstraction.Cases.StateFR.Error;

        var refused = await sut.ProtocolTechnicalEvent0x3001Async(TechnicalEvent(Json("{\"FR\":{\"Code\":\"90\",\"Message\":\"Archivage\"}}")));

        refused.receiptResponse.ftState.Should().Be((State) StateFR.Error);
        refused.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE")
            .Which.Data.Should().Contain("technical event journal");

        sscd.FailWith = null;
        var next = await sut.ProtocolTechnicalEvent0x3001Async(TechnicalEvent(Json("{\"FR\":{\"Code\":\"90\",\"Message\":\"Archivage\"}}")));
        next.receiptResponse.ftReceiptIdentification.Should().Be("ft1#L1", "the refused event consumed no number of chain L");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{\"FR\":{\"Code\":\"11\",\"Message\":\"x\"}}")]
    [InlineData("{\"FR\":{\"Code\":\"90\"}}")]
    public async Task MalformedTechnicalEvent_IsRefusedByTheQueue_BeforeAnyScu(string? caseData)
    {
        var (sut, sscd) = CreateSut();

        var refused = await sut.ProtocolTechnicalEvent0x3001Async(TechnicalEvent(caseData is null ? null : Json(caseData)));

        refused.receiptResponse.ftState.Should().Be((State) StateFR.Error);
        sscd.Calls.Should().BeEmpty("the LNE and self-certified SCUs must refuse the same events as InfoCert");
        refused.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
    }

    private static ProcessCommandRequest AccountingEvent(int code)
    {
        var request = TechnicalEvent(Json($"{{\"FR\":{{\"Code\":\"{code}\",\"Message\":\"Événement\"}}}}"));
        request.ReceiptRequest.ftReceiptCase = ReceiptCase.ProtocolAccountingEvent0x3002.WithCountry("FR");
        return request;
    }

    /// <summary>ADR 0021: 0x3002 (Protocol, audit / accounting event) accepts R19 160, 180 or an editor code from 900 up.</summary>
    [Theory]
    [InlineData(160)]
    [InlineData(180)]
    [InlineData(900)]
    public async Task AccountingEvent_WithAnAccountingCode_IsSignedIntoTheLogChain(int code)
    {
        var (sut, sscd) = CreateSut();

        var response = await sut.ProtocolAccountingEvent0x3002Async(AccountingEvent(code));

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#L1");
        sscd.Calls.Should().ContainSingle();
    }

    [Theory]
    [InlineData(20)]
    [InlineData(260)]
    [InlineData(11)]
    public async Task AccountingEvent_WithAnyOtherCode_IsRefusedByTheQueue_BeforeAnyScu(int code)
    {
        var (sut, sscd) = CreateSut();

        var refused = await sut.ProtocolAccountingEvent0x3002Async(AccountingEvent(code));

        refused.receiptResponse.ftState.Should().Be((State) StateFR.Error);
        sscd.Calls.Should().BeEmpty("every SCU refuses the same accounting events");
        refused.receiptResponse.ftReceiptIdentification.Should().Be("ft1#");
    }

    [Fact]
    public async Task AccountingEvent_WithoutEvent_IsRefusedByTheQueue()
    {
        var (sut, sscd) = CreateSut();
        var request = TechnicalEvent(null);
        request.ReceiptRequest.ftReceiptCase = ReceiptCase.ProtocolAccountingEvent0x3002.WithCountry("FR");

        var refused = await sut.ProtocolAccountingEvent0x3002Async(request);

        refused.receiptResponse.ftState.Should().Be((State) StateFR.Error);
        sscd.Calls.Should().BeEmpty();
    }

    /// <summary>ADR 0021: a training 0x3001 is signed as an R19 JET string on chain X, so its event is checked too.</summary>
    [Fact]
    public async Task MalformedTrainingTechnicalEvent_IsRefusedByTheQueue()
    {
        var (sut, sscd) = CreateSut();
        var request = TechnicalEvent(null);
        request.ReceiptRequest.ftReceiptCase = request.ReceiptRequest.ftReceiptCase.WithFlag(ReceiptCaseFlags.Training);

        var refused = await sut.ProtocolTechnicalEvent0x3001Async(request);

        refused.receiptResponse.ftState.Should().Be((State) StateFR.Error);
        sscd.Calls.Should().BeEmpty();
    }
}
