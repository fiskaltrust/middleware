using System.Security.Cryptography;
using FluentAssertions;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.SCU.FR.SelfCertified;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>The same scenarios against the self-certified SCU (market-fr#176), with a fresh P-256 key.</summary>
public class SelfCertifiedFRScenarioTests : FRScenarioTests
{
    protected override IFRSSCD CreateScu()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new SelfCertifiedFRSSCD(new SelfCertifiedConfiguration
        {
            Siret = "12345678901234",
            PrivateKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
            CertificateSerialNumber = "SELF-0001",
        });
    }

    /// <summary>
    /// The self-certified SCU, like LNE, prints no total items: the totals are field 12 of the
    /// signed data set, as <c>period,current totalizer,...,perpetual totalizer,...</c> (11 amounts each).
    /// </summary>
    protected override (string? Day, string? Perpetual) TotalsOf(ReceiptResponse closing)
    {
        var totals = SignatureOf(closing, SignatureTypeFR.Information)!.Split('|')[11].Split(',');
        totals[0].Should().Be("Day");
        return (totals[1], totals[12]);
    }
}
