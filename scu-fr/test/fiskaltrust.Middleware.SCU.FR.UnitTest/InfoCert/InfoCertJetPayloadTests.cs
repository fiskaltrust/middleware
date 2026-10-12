using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest.InfoCert;

/// <summary>The signed content of a technical event (JET, R19 v4.2 "Signature" sheet), market-fr#200.</summary>
public class InfoCertJetPayloadTests
{
    private static InfoCertConfiguration Configuration(string privateKeyBase64) => new()
    {
        Siret = FRTestData.Siret,
        PrivateKey = privateKeyBase64,
        CertificateSerialNumber = "CERT-4711",
        AttestationNumber = "IC-2026-001",
        SoftwareVersion = "1.0.0",
    };

    private static ProcessRequest TechnicalEvent(string caseData, object? cbUser, long chainCounter, ReceiptCase receiptCase = ReceiptCase.ProtocolTechnicalEvent0x3001)
    {
        var request = FRTestData.Request(chainCounter);
        request.ReceiptRequest.ftReceiptCase = receiptCase.WithCountry("FR");
        request.ReceiptRequest.ftReceiptCaseData = JsonDocument.Parse(caseData).RootElement.Clone();
        request.ReceiptRequest.cbUser = cbUser;
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];
        return request;
    }

    private static async Task<(JsonElement payload, string signature)> SignAsync(ProcessRequest request, string? previousSignature)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(Configuration(privateKey));
        var (response, signature) = await sut.ProcessReceiptAsync(request, previousSignature);
        var jws = response.ReceiptResponse.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data;
        using var document = JsonDocument.Parse(FRTestData.FromBase64Url(jws.Split('.')[1]));
        return (document.RootElement.Clone(), signature);
    }

    [Fact]
    public async Task TechnicalEvent_SignsTheR19JetFieldsInTheSignatureSheetOrder()
    {
        var request = TechnicalEvent("{\"Code\":\"90\",\"Message\":\"Archivage\",\"Information\":\"periode 2026-02\"}", "Alice", 3);

        var (payload, _) = await SignAsync(request, "cHJldmlvdXM");

        payload.EnumerateObject().Select(x => x.Name).Should().Equal(
            "queueItemId", "eventCode", "information", "receiptMoment", "operatorId",
            "cashBoxIdentification", "previousSignatureCarriedOver", "previousSignature", "chainCounter");
        payload.GetProperty("queueItemId").GetGuid().Should().Be(request.ReceiptResponse.ftQueueItemID);
        payload.GetProperty("eventCode").GetInt32().Should().Be(90);
        payload.GetProperty("information").GetString().Should().Be("periode 2026-02", "TAG-JET-LIB signs the contextual information, not the label");
        payload.GetProperty("receiptMoment").GetDateTime().Should().Be(request.ReceiptResponse.ftReceiptMoment);
        payload.GetProperty("operatorId").GetString().Should().Be("Alice");
        payload.GetProperty("cashBoxIdentification").GetString().Should().Be("cashbox-fr");
        payload.GetProperty("previousSignatureCarriedOver").GetString().Should().Be("O");
        payload.GetProperty("previousSignature").GetString().Should().Be("cHJldmlvdXM");
        payload.GetProperty("chainCounter").GetInt64().Should().Be(3);
    }

    [Fact]
    public async Task FirstTechnicalEvent_StartsTheChainWithEmptyOptionalFields()
    {
        var (payload, _) = await SignAsync(TechnicalEvent("{\"Code\":\"15\",\"Message\":\"Acces JET\"}", null, 1), null);

        payload.GetProperty("previousSignatureCarriedOver").GetString().Should().Be("N");
        payload.GetProperty("previousSignature").GetString().Should().BeEmpty();
        payload.GetProperty("information").GetString().Should().BeEmpty();
        payload.GetProperty("operatorId").GetString().Should().BeEmpty();
    }

    [Theory]
    [InlineData(ReceiptCase.ProtocolUnspecified0x3000)]
    [InlineData(ReceiptCase.ProtocolAccountingEvent0x3002)]
    public async Task OtherLogCases_KeepTheReceiptDataSet(ReceiptCase receiptCase)
    {
        var (payload, _) = await SignAsync(TechnicalEvent("{\"Code\":\"90\",\"Message\":\"x\"}", null, 1, receiptCase), null);

        payload.TryGetProperty("eventCode", out _).Should().BeFalse();
        payload.TryGetProperty("receiptCase", out _).Should().BeTrue();
    }
}
