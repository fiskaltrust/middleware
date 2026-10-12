using System.Security.Cryptography;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>The same scenarios against the self-certified SCU (market-fr#176), with a fresh P-256 key.</summary>
public class SelfCertifiedFRScenarioTests : FRScenarioTests
{
    protected override IFRSSCD CreateScu() => Create("SELF-0001");

    /// <summary>A key renewal: same SCU type, new key and certificate serial.</summary>
    protected override IFRSSCD CreateIncomingScu() => Create("SELF-0002");

    internal static IFRSSCD Create(string certificateSerialNumber)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new SelfCertifiedFRSSCD(new SelfCertifiedConfiguration
        {
            Siret = "12345678901234",
            PrivateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
            CertificateSerialNumber = certificateSerialNumber,
        });
    }

    /// <summary>The self-certified SCU, like LNE, prints no total items: they are in the signed data set.</summary>
    protected override (string? Day, string? Perpetual) TotalsOf(ReceiptResponse closing) => TotalsFromSignedDataSet(closing);

    protected override string? PreviousSignatureCoveredBy(ReceiptResponse response) => LastFieldOfSignedDataSet(response);
}
