using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
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

    [Fact]
    public async Task TrainingTechnicalEvent_IsNotCheckedAsAJournalEntry()
    {
        var (response, signature) = await SignAsync(TechnicalEvent(null, training: true));

        response.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        signature.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(InfoCertJetEvent.ContinuousSequenceModeChange)]
    [InlineData(InfoCertJetEvent.JetAccess)]
    [InlineData(90)]
    [InlineData(450)]
    [InlineData(900)]
    [InlineData(4711)]
    public void R19AndEditorCodes_AreValid(int code) => InfoCertJetEvent.IsValidCode(code).Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(310)]
    [InlineData(899)]
    [InlineData(-90)]
    public void OtherCodes_AreInvalid(int code) => InfoCertJetEvent.IsValidCode(code).Should().BeFalse();

    [Fact]
    public void ContinuousSequenceModeChange_FollowsTheArchiveContract()
        => InfoCertJetEvent.ContinuousSequenceModeChange.Should().Be(10, "the archive contract (market-fr#227) lists 10 first; the choice 10/15 is open in market-fr#200");

    [Fact]
    public void TryRead_AcceptsTheV1KeysUnderFR_CaseInsensitive_AsStringOrNumberCode_AndTakesTheOperatorFromCbUser()
    {
        var request = TechnicalEvent(Json("{\"fr\":{\"code\":90,\"message\":\"Archivage\",\"information\":\"\"}}"), Json("\"Alice\"")).ReceiptRequest;

        InfoCertJetEvent.TryRead(request, out var jetEvent, out var error).Should().BeTrue(error);

        jetEvent.Should().Be(new InfoCertJetEvent(90, "Archivage", null, "Alice"));
    }

    [Fact]
    public void TryRead_AcceptsCaseDataSentAsAJsonString()
    {
        var request = TechnicalEvent("{\"FR\":{\"Code\":\"15\",\"Message\":\"Acces JET\"}}").ReceiptRequest;

        InfoCertJetEvent.TryRead(request, out var jetEvent, out _).Should().BeTrue();

        jetEvent!.Code.Should().Be(InfoCertJetEvent.JetAccess);
        jetEvent.Operator.Should().BeNull();
    }
}
