using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Helpers;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.Middleware.SCU.FR.LNE;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>
/// The signature items each FR SCU returns through a real queue, printed to the test output so a
/// reviewer sees what a POS prints. Every SCU returns the receipt QR code as v1.2 did (type 0x…01,
/// format QR code, caption www.fiskaltrust.fr) with the signed content and its signature, the
/// certificate serial inside the signed content, and no separate certificate item.
/// </summary>
public class SignatureItemsTests
{
    /// <summary>Byte-mode capacity of a version 40 QR code at error correction level M.</summary>
    private const int QrCapacityV40M = 2331;

    private readonly ITestOutputHelper _output;

    public SignatureItemsTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("LNE")]
    [InlineData("InfoCert")]
    [InlineData("SelfCertified")]
    public async Task SaleAndDailyClosing_ReturnTheQrCodeWithTheCertificateSerialSigned(string scuName)
    {
        var (signProcessor, cashBoxId) = FRTestQueue.Create(CreateScu(scuName));

        foreach (var request in new[] { CashSale(cashBoxId), Request(cashBoxId, ReceiptCase.DailyClosing0x2011) })
        {
            var response = JsonSerializer.Deserialize<ReceiptResponse>(await signProcessor(JsonSerializer.Serialize(request)))!;
            Print(scuName, response);

            response.ftState.IsState(State.Error).Should().BeFalse();
            var qr = response.ftSignatures.Single(x => ((long) x.ftSignatureType & 0xFFFF) == 0x0001);
            qr.ftSignatureFormat.Should().Be(SignatureFormat.QRCode);
            qr.Caption.Should().Be("www.fiskaltrust.fr");
            Encoding.UTF8.GetByteCount(qr.Data).Should().BeLessThan(QrCapacityV40M, "the content must fit one QR code");
            FRChaining.ChainedSignatureOf(qr.Data).Should().MatchRegex("^[A-Za-z0-9_-]+$");
            CertificateSerialOf(qr.Data).Should().Be($"{scuName}-0001");
            response.ftSignatures.Should().NotContain(x => ((long) x.ftSignatureType & 0xFFFF) == 0x0002, "the certificate serial has no item of its own, as in v1.2");
            response.ftSignatures.Should().NotContain(x => x.Caption == "Donnees signees", "the data set is in the QR code");
        }
    }

    /// <summary>The serial, where v1.2 put it: the JWS payload's <c>csn</c>, or the data set's fourth-from-last field.</summary>
    private static string? CertificateSerialOf(string qrCode)
    {
        var signedContent = FRReceiptQrCode.SignedContentOf(qrCode);
        if (signedContent.Contains('|'))
        {
            return signedContent.Split('|')[^4];
        }
        var payload = signedContent.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var json = JsonDocument.Parse(Convert.FromBase64String(payload));
        return json.RootElement.GetProperty("csn").GetString();
    }

    private void Print(string scuName, ReceiptResponse response)
    {
        _output.WriteLine($"== {scuName} {response.ftReceiptIdentification}");
        foreach (var item in response.ftSignatures)
        {
            _output.WriteLine($"0x{(long) item.ftSignatureType:X16} {item.ftSignatureFormat,-8} [{item.Caption}] {item.Data} ({Encoding.UTF8.GetByteCount(item.Data)} B)");
        }
    }

    private static IFRSSCD CreateScu(string scuName)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey());
        const string siret = "12345678901234";
        return scuName switch
        {
            "LNE" => new LneFRSSCD(new LneConfiguration { Siret = siret, PrivateKey = privateKey, CertificateSerialNumber = "LNE-0001", LneCertificateNumber = "LNE-2026-042" }),
            "InfoCert" => new InfoCertFRSSCD(new InfoCertConfiguration { Siret = siret, PrivateKey = privateKey, CertificateSerialNumber = "InfoCert-0001", AttestationNumber = "IC-2026-001" }),
            _ => new SelfCertifiedFRSSCD(new SelfCertifiedConfiguration { Siret = siret, PrivateKey = privateKey, CertificateSerialNumber = "SelfCertified-0001" }),
        };
    }

    private static ReceiptRequest Request(Guid cashBoxId, ReceiptCase receiptCase) => new()
    {
        ftCashBoxID = cashBoxId,
        cbTerminalID = "1",
        cbUser = "operator-1",
        cbReceiptReference = Guid.NewGuid().ToString("N")[..8],
        cbReceiptMoment = DateTime.UtcNow,
        Currency = Currency.EUR,
        ftReceiptCase = receiptCase.WithCountry("FR"),
        cbChargeItems = [],
        cbPayItems = [],
    };

    private static ReceiptRequest CashSale(Guid cashBoxId)
    {
        var request = Request(cashBoxId, ReceiptCase.PointOfSaleReceipt0x0001);
        request.cbChargeItems =
        [
            new ChargeItem { Amount = 12.00m, VATAmount = 2.00m, VATRate = 20m, Quantity = 1, Description = "Cafe", Currency = Currency.EUR, ftChargeItemCase = ChargeItemCase.NormalVatRate.WithCountry("FR") },
        ];
        request.cbPayItems =
        [
            new PayItem { Amount = 12.00m, Description = "Especes", Currency = Currency.EUR, ftPayItemCase = PayItemCase.CashPayment.WithCountry("FR") },
        ];
        return request;
    }
}
