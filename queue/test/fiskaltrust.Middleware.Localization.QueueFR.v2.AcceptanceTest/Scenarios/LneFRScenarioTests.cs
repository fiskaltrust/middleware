using System.Security.Cryptography;
using FluentAssertions;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.fr;
using fiskaltrust.Middleware.SCU.FR.Abstraction.Helpers;
using fiskaltrust.Middleware.Localization.QueueFR.v2.Models;
using fiskaltrust.Middleware.SCU.FR.LNE;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.AcceptanceTest.Scenarios;

/// <summary>The same scenarios against the LNE SCU, with a fresh P-256 key.</summary>
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

    /// <summary>
    /// LNE prints no total items: the totals are the fifth field from the end of the signed data
    /// set in the QR code, as <c>period,current totalizer,...,perpetual totalizer,...</c> (11 amounts each).
    /// </summary>
    protected override (string? Day, string? Perpetual) TotalsOf(ReceiptResponse closing)
    {
        var dataSet = FRReceiptQrCode.SignedContentOf(closing.ftSignatures.Single(x => x.ftSignatureType.IsType(SignatureTypeFR.ReceiptSignature)).Data);
        var totals = dataSet.Split('|')[^5].Split(',');
        totals[0].Should().Be("Day");
        return (totals[1], totals[12]);
    }
}
