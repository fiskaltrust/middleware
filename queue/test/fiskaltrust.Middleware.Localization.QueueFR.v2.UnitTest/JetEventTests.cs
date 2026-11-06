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
/// SCU decides whether an event can be signed, the queue only acts on its response.
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

        var refused = await sut.ProtocolTechnicalEvent0x3001Async(TechnicalEvent(null));

        refused.receiptResponse.ftState.Should().Be((State) StateFR.Error);
        refused.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE")
            .Which.Data.Should().Contain("technical event journal");

        sscd.FailWith = null;
        var next = await sut.ProtocolTechnicalEvent0x3001Async(TechnicalEvent(Json("{\"FR\":{\"Code\":\"90\",\"Message\":\"Archivage\"}}")));
        next.receiptResponse.ftReceiptIdentification.Should().Be("ft1#L1", "the refused event consumed no number of chain L");
    }
}
