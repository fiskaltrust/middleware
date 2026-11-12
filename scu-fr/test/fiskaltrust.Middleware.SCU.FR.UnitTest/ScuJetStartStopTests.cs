using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Cases;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.Middleware.SCU.FR.LNE;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>
/// JET 80, 40 and 127 around queue start and stop (contract §6): the POS may send 80, 40 and 127 as
/// a 0x3001 (Protocol (technical event)) and every FR SCU signs them; only the InfoCert SCU raises 80
/// itself, as a JET item on the 0x4001 (Queue-start receipt) response; nothing raises 40 or 127 on
/// 0x4002 (Queue-stop receipt), the POS sends them before the last archive receipt.
/// </summary>
public class ScuJetStartStopTests
{
    public static TheoryData<string, int> ScusAndCodes()
    {
        var data = new TheoryData<string, int>();
        foreach (var scu in new[] { "LNE", "SelfCertified", "InfoCert" })
        {
            foreach (var code in new[] { 80, 40, 127 })
            {
                data.Add(scu, code);
            }
        }

        return data;
    }

    private static IFRSSCD Scu(string scu, string privateKey) => scu switch
    {
        "LNE" => new LneFRSSCD(new LneConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "C", LneCertificateNumber = "L" }),
        "SelfCertified" => new SelfCertifiedFRSSCD(new SelfCertifiedConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "C" }),
        _ => new InfoCertFRSSCD(new InfoCertConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "C" }),
    };

    private static ProcessRequest Request(ReceiptCase receiptCase, bool training = false, object? caseData = null)
    {
        var request = FRTestData.Request();
        var withCountry = receiptCase.WithCountry("FR");
        request.ReceiptRequest.ftReceiptCase = training ? withCountry.WithFlag(ReceiptCaseFlags.Training) : withCountry;
        request.ReceiptRequest.ftReceiptCaseData = caseData;
        request.ReceiptRequest.cbChargeItems = [];
        request.ReceiptRequest.cbPayItems = [];
        return request;
    }

    private static SignatureItem[] JetItems(ProcessResponse response)
        => response.ReceiptResponse.ftSignatures.Where(x => ((long) x.ftSignatureType & 0xFFFF) == ((long) SignatureTypeFR.JetEvent & 0xFFFF)).ToArray();

    [Theory]
    [MemberData(nameof(ScusAndCodes))]
    public async Task EveryScu_SignsJet80_40_127_SentByThePos(string scu, int code)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var sut = Scu(scu, privateKey);

        var caseData = JsonDocument.Parse($"{{\"FR\":{{\"Code\":\"{code}\",\"Message\":\"event {code}\"}}}}").RootElement.Clone();
        var (response, signature) = await sut.ProcessReceiptAsync(Request(ReceiptCase.ProtocolTechnicalEvent0x3001, caseData: caseData), null);

        response.ReceiptResponse.ftState.IsState(State.Error).Should().BeFalse();
        signature.Should().NotBeEmpty();
    }

    [Fact]
    public async Task InfoCert_RaisesJet80_OnQueueStart()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        using var sut = new InfoCertFRSSCD(new InfoCertConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "C" });

        var request = Request(ReceiptCase.InitialOperationReceipt0x4001);
        request.ReceiptRequest.cbReceiptReference = "start-1";
        var (response, _) = await sut.ProcessReceiptAsync(request, null);

        var item = JetItems(response).Should().ContainSingle().Which;
        item.Caption.Should().Be("JET");
        var fr = JsonDocument.Parse(item.Data).RootElement.GetProperty("FR");
        fr.GetProperty("Code").GetString().Should().Be("80");
        fr.GetProperty("Message").GetString().Should().Be(InfoCertFRSSCD.TerminalStartJetLabel);
        fr.GetProperty("Information").GetString().Should().Contain("cbReceiptReference=start-1");
    }

    [Theory]
    [InlineData("InfoCert", 0x4002L, false)]
    [InlineData("InfoCert", 0x4001L, true)]
    [InlineData("LNE", 0x4001L, false)]
    [InlineData("LNE", 0x4002L, false)]
    [InlineData("SelfCertified", 0x4001L, false)]
    [InlineData("SelfCertified", 0x4002L, false)]
    public async Task NoOtherScuOrCase_RaisesAJetItem(string scu, long receiptCase, bool training)
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var sut = Scu(scu, privateKey);

        var (response, _) = await sut.ProcessReceiptAsync(Request((ReceiptCase) receiptCase, training), null);

        JetItems(response).Should().BeEmpty();
    }
}
