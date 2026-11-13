using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest.InfoCert;

/// <summary>
/// Which technical events the InfoCert SCU accepts as JET entries (R19 v4.2, market-fr#200). The
/// decision is the SCU's: it refuses a malformed event in its response, and the queue only sees
/// the refusal.
/// </summary>
public class InfoCertJetEventTests
{
    private static ProcessRequest TechnicalEvent(object? caseData, object? cbUser = null, bool training = false)
    {
        var request = FRTestData.Request();
        var receiptCase = ReceiptCase.ProtocolTechnicalEvent0x3001.WithCountry("FR");
        request.ReceiptRequest.ftReceiptCase = training ? receiptCase.WithFlag(ReceiptCaseFlags.Training) : receiptCase;
        request.ReceiptRequest.ftReceiptCaseData = caseData;
        request.ReceiptRequest.cbUser = cbUser;
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];
        return request;
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static async Task<(ProcessResponse response, string signature)> SignAsync(ProcessRequest request)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(new InfoCertConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "CERT-4711" });
        return await sut.ProcessReceiptAsync(request, null);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{\"FR\":{\"Message\":\"no code\"}}")]
    [InlineData("{\"FR\":{\"Code\":\"11\",\"Message\":\"not an R19 code\"}}")]
    [InlineData("{\"FR\":{\"Code\":\"abc\",\"Message\":\"not a number\"}}")]
    [InlineData("{\"FR\":{\"Code\":\"90\"}}")]
    [InlineData("{\"Code\":\"90\",\"Message\":\"top level, not under FR\"}")]
    [InlineData("{\"FR\":\"90\"}")]
    [InlineData("[1,2]")]
    public async Task MalformedTechnicalEvent_IsRefusedInTheResponse(string? caseData)
    {
        var (response, signature) = await SignAsync(TechnicalEvent(caseData is null ? null : Json(caseData)));

        response.ReceiptResponse.ftState.Should().Be((State) StateFR.Error, "a refusal, not an unavailable signing unit");
        response.ReceiptResponse.ftSignatures.Should().ContainSingle(x => x.Caption == "FAILURE")
            .Which.Data.Should().Contain("technical event journal");
        signature.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidTechnicalEvent_IsSigned()
    {
        var (response, signature) = await SignAsync(TechnicalEvent(Json("{\"FR\":{\"Code\":\"90\",\"Message\":\"Archivage\",\"Information\":\"Z12\"}}"), "Alice"));

        response.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        signature.Should().NotBeEmpty();
    }

    /// <summary>ADR 0021: a training 0x3001 is signed as an R19 JET string on chain X too, so its event must read.</summary>
    [Fact]
    public async Task TrainingTechnicalEvent_IsCheckedLikeAnyEvent()
    {
        var (response, signature) = await SignAsync(TechnicalEvent(null, training: true));

        response.ReceiptResponse.ftState.Should().Be((State) StateFR.Error);
        signature.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidTrainingTechnicalEvent_IsSigned()
    {
        var (response, signature) = await SignAsync(TechnicalEvent(Json("{\"FR\":{\"Code\":\"90\",\"Message\":\"Archivage\"}}"), training: true));

        response.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        signature.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(160, true)]
    [InlineData(180, true)]
    [InlineData(900, true)]
    [InlineData(20, false)]
    [InlineData(260, false)]
    [InlineData(11, false)]
    public void AccountingEvent_AcceptsOnlyAccountingCodes(int code, bool accepted)
    {
        var request = TechnicalEvent(Json($"{{\"FR\":{{\"Code\":\"{code}\",\"Message\":\"Événement\"}}}}")).ReceiptRequest;
        request.ftReceiptCase = ReceiptCase.ProtocolAccountingEvent0x3002.WithCountry("FR");

        FRJetEvent.TryReadFor(request, out _, out var error).Should().Be(accepted, error);
    }

    [Fact]
    public async Task AccountingEventWithATechnicalCode_IsRefusedInTheResponse()
    {
        var request = TechnicalEvent(Json("{\"FR\":{\"Code\":\"20\",\"Message\":\"Archivage\"}}"));
        request.ReceiptRequest.ftReceiptCase = ReceiptCase.ProtocolAccountingEvent0x3002.WithCountry("FR");

        var (response, signature) = await SignAsync(request);

        response.ReceiptResponse.ftState.Should().Be((State) StateFR.Error);
        signature.Should().BeEmpty();
    }

    [Fact]
    public void TryReadFor_LeavesTheTechnicalEventRulesUnchanged()
    {
        var request = TechnicalEvent(Json("{\"FR\":{\"Code\":\"20\",\"Message\":\"Archivage\"}}")).ReceiptRequest;

        FRJetEvent.TryReadFor(request, out var jetEvent, out _).Should().BeTrue();
        jetEvent!.Code.Should().Be(20);
    }

    [Theory]
    [InlineData(FRJetEvent.ContinuousSequenceModeChange)]
    [InlineData(15)]
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
        => FRJetEvent.ContinuousSequenceModeChange.Should().Be(10, "R19 v4 gave 10 this meaning and v2 never used 10 for JET access (market-fr#200, 2026-11-04)");

    [Fact]
    public void TryRead_AcceptsTheV1KeysUnderFR_CaseInsensitive_AsStringOrNumberCode_AndTakesTheOperatorFromCbUser()
    {
        var request = TechnicalEvent(Json("{\"fr\":{\"code\":90,\"message\":\"Archivage\",\"information\":\"\"}}"), Json("\"Alice\"")).ReceiptRequest;

        FRJetEvent.TryRead(request, out var jetEvent, out var error).Should().BeTrue(error);

        jetEvent.Should().Be(new FRJetEvent(90, "Archivage", null, "Alice"));
    }

    [Fact]
    public void TryRead_AcceptsCaseDataSentAsAJsonString()
    {
        var request = TechnicalEvent("{\"FR\":{\"Code\":\"15\",\"Message\":\"Acces JET\"}}").ReceiptRequest;

        FRJetEvent.TryRead(request, out var jetEvent, out _).Should().BeTrue();

        jetEvent!.Code.Should().Be(15);
        jetEvent.Operator.Should().BeNull();
    }
}
