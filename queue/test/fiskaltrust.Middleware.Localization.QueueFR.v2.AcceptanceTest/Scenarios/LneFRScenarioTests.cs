using System.Security.Cryptography;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.LNE;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>
/// The same scenarios against the LNE SCU; its SCU switch moves to the self-certified SCU, i.e.
/// to another certification body as well as another certificate (market-fr#177).
/// </summary>
public class LneFRScenarioTests : FRScenarioTests
{
    protected override IFRSSCD CreateScu()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new LneFRSSCD(new LneConfiguration
        {
            Siret = "12345678901234",
            PrivateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
            CertificateSerialNumber = "LNE-0001",
            LneCertificateNumber = "LNE-2026-042",
        });
    }

    protected override IFRSSCD CreateIncomingScu() => SelfCertifiedFRScenarioTests.Create("SELF-0001");

    protected override (string? Day, string? Perpetual) TotalsOf(ReceiptResponse closing) => TotalsFromSignedDataSet(closing);

    protected override string? PreviousSignatureCoveredBy(ReceiptResponse response) => LastFieldOfSignedDataSet(response);
}
