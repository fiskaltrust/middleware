using System.Text.Json;
using fiskaltrust.ifPOS.v2.fr;

namespace fiskaltrust.Middleware.SCU.FR.InfoCert.Models;

/// <summary>
/// The state the Infocert SCU reports through <see cref="FRSSCDInfo.InfoData"/>. The queue reads
/// individual properties out of the json blob instead of referencing this package.
/// </summary>
public class InfoCertScuInfo
{
    /// <summary>Always "Infocert" — the certification body this SCU implements.</summary>
    public string CertificationBody { get; set; } = InfoCertFRSSCD.CertificationBody;

    public string Siret { get; set; } = "";

    public string CertificateSerialNumber { get; set; } = "";

    public string AttestationNumber { get; set; } = "";

    public string SoftwareName { get; set; } = "";

    public string SoftwareVersion { get; set; } = "";

    /// <summary>The issuer claims as configured (market-fr#141), reported for the archive's compliance parameters (contract §12).</summary>
    public string? Rcs { get; set; }

    public string? Naf { get; set; }

    public string? LegalForm { get; set; }

    public string? VatOption { get; set; }

    public string? ElectronicAddress { get; set; }

    /// <summary>The NF525 software category (A, B or C) as configured.</summary>
    public string? SoftwareCategory { get; set; }

    /// <summary>True once the configured key material was loaded successfully.</summary>
    public bool SignatureCreationDataAvailable { get; set; }

    public FRSSCDInfo ToFRSSCDInfo() => new()
    {
        Description = $"fiskaltrust Middleware SCU FR (Infocert), SIRET {Siret}",
        Version = SoftwareVersion,
        InfoData = JsonSerializer.Serialize(this),
    };
}
