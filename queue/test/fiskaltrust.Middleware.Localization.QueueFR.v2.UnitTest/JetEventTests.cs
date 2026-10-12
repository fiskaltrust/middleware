using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Logic;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.UnitTest;

/// <summary>Technical events of the NF525 JET (Infocert R19 v4.2), market-fr#200.</summary>
public class JetEventTests
{
    private static (ProtocolCommandProcessorFR sut, FakeFRSSCD sscd) CreateSut()
    {
        var sscd = new FakeFRSSCD();
        var repository = new Mock<IMiddlewareQueueItemRepository>();
        repository.Setup(x => x.GetAsync()).ReturnsAsync(Array.Empty<ftQueueItem>());
        var pipeline = new FRSigningPipeline(sscd, new FRChainStateProvider(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(repository.Object))));
        return (new ProtocolCommandProcessorFR(pipeline), sscd);
    }

    private static ProcessCommandRequest TechnicalEvent(object? caseData, object? cbUser = null, bool training = false)
    {
        var receiptCase = ReceiptCase.ProtocolTechnicalEvent0x3001.WithCountry("FR");
        return new(
            new ftQueue { ftQueueId = Guid.NewGuid() },
            new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = training ? receiptCase.WithFlag(ReceiptCaseFlags.Training) : receiptCase,
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
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public async Task ValidTechnicalEvent_IsSignedIntoTheLogChain()
    {
        var (sut, sscd) = CreateSut();

        var response = await sut.ProtocolTechnicalEvent0x3001Async(TechnicalEvent(Json("{\"Code\":\"90\",\"Message\":\"Archivage\",\"Information\":\"Z12\"}"), "Alice"));

        response.receiptResponse.ftReceiptIdentification.Should().Be("ft1#L1");
        sscd.Calls.Should().ContainSingle();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{\"Message\":\"no code\"}")]
    [InlineData("{\"Code\":\"11\",\"Message\":\"not an R19 code\"}")]
    [InlineData("{\"Code\":\"abc\",\"Message\":\"not a number\"}")]
    [InlineData("{\"Code\":\"90\"}")]
    [InlineData("[1,2]")]
    public async Task MalformedTechnicalEvent_IsRejectedWithoutTakingANumber(string? caseData)
    {
        var (sut, sscd) = CreateSut();

        var response = await sut.ProtocolTechnicalEvent0x3001Async(TechnicalEvent(caseData is null ? null : Json(caseData)));

        sscd.Calls.Should().BeEmpty("a rejected event must not consume a number of chain L");
        response.receiptResponse.ftState.IsState(State.Error).Should().BeTrue();
        response.receiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE");
    }

    [Fact]
    public async Task TrainingTechnicalEvent_IsNotValidatedAsAJournalEntry()
    {
        var (sut, sscd) = CreateSut();

        var response = await sut.ProtocolTechnicalEvent0x3001Async(TechnicalEvent(null, training: true));

        response.receiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        sscd.Calls.Should().ContainSingle();
    }

    [Theory]
    [InlineData(FRJetEvent.ContinuousSequenceModeChange)]
    [InlineData(FRJetEvent.JetAccess)]
    [InlineData(90)]
    [InlineData(450)]
    [InlineData(900)]
    [InlineData(4711)]
    public void R19AndEditorCodes_AreValid(int code) => FRJetEvent.IsValidCode(code).Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(310)]
    [InlineData(899)]
    [InlineData(-90)]
    public void OtherCodes_AreInvalid(int code) => FRJetEvent.IsValidCode(code).Should().BeFalse();

    [Fact]
    public void ContinuousSequenceModeChange_FollowsTheArchiveContract()
        => FRJetEvent.ContinuousSequenceModeChange.Should().Be(10, "the archive contract (market-fr#227) lists 10 first; the choice 10/15 is open in market-fr#200");

    [Fact]
    public void TryRead_AcceptsTheV1CaseDataFormat_CaseInsensitive_AsStringOrNumberCode_AndTakesTheOperatorFromCbUser()
    {
        var request = TechnicalEvent(Json("{\"code\":90,\"message\":\"Archivage\",\"information\":\"\"}"), Json("\"Alice\"")).ReceiptRequest;

        FRJetEvent.TryRead(request, out var jetEvent, out var error).Should().BeTrue(error);

        jetEvent.Should().Be(new FRJetEvent(90, "Archivage", null, "Alice"));
    }

    [Fact]
    public void TryRead_AcceptsCaseDataSentAsAJsonString()
    {
        var request = TechnicalEvent("{\"Code\":\"15\",\"Message\":\"Acces JET\"}").ReceiptRequest;

        FRJetEvent.TryRead(request, out var jetEvent, out _).Should().BeTrue();

        jetEvent!.Code.Should().Be(FRJetEvent.JetAccess);
        jetEvent.Operator.Should().BeNull();
    }
}
