using System.Text.Json;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.InfoCert;
using fiskaltrust.Middleware.SCU.FR.LNE;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest;

/// <summary>
/// Every configuration key of an FR SCU except secrets reaches the archive's compliance parameters through
/// <c>InfoData</c> (archive-v2 §12): the issuer claims of every SCU, and the InfoCert software category.
/// </summary>
public class ScuInfoTests
{
    private static async Task<JsonElement> InfoData(IFRSSCD scu) => JsonDocument.Parse((await scu.GetInfoAsync()).InfoData!).RootElement;

    [Fact]
    public async Task EverySCU_ReportsItsIssuerClaims()
    {
        var (privateKey, key) = FRTestData.CreateKey();
        using var _ = key;
        var scus = new IFRSSCD[]
        {
            new LneFRSSCD(new LneConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "C", LneCertificateNumber = "L", Rcs = "RCS Paris B 1", Naf = "62.01Z", LegalForm = "SAS", VatOption = "debits", ElectronicAddress = "123456789" }),
            new SelfCertifiedFRSSCD(new SelfCertifiedConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "C", Rcs = "RCS Paris B 1", Naf = "62.01Z", LegalForm = "SAS", VatOption = "debits", ElectronicAddress = "123456789" }),
            new InfoCertFRSSCD(new InfoCertConfiguration { Siret = FRTestData.Siret, PrivateKey = privateKey, CertificateSerialNumber = "C", Rcs = "RCS Paris B 1", Naf = "62.01Z", LegalForm = "SAS", VatOption = "debits", ElectronicAddress = "123456789", SoftwareCategory = "B" }),
        };

        foreach (var scu in scus)
        {
            var info = await InfoData(scu);
            info.GetProperty("Rcs").GetString().Should().Be("RCS Paris B 1");
            info.GetProperty("Naf").GetString().Should().Be("62.01Z");
            info.GetProperty("LegalForm").GetString().Should().Be("SAS");
            info.GetProperty("VatOption").GetString().Should().Be("debits");
            info.GetProperty("ElectronicAddress").GetString().Should().Be("123456789");
            info.TryGetProperty("PrivateKey", out var _).Should().BeFalse();
        }

        (await InfoData(scus[2])).GetProperty("SoftwareCategory").GetString().Should().Be("B");
    }
}
