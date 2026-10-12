using System.Text.Json;
using fiskaltrust.ifPOS.v2.fr;

namespace fiskaltrust.Middleware.SCU.FR.SelfCertified.Models;

/// <summary>
/// The state the self-certified SCU reports through <see cref="FRSSCDInfo.InfoData"/>. The queue
/// reads individual properties out of the json blob instead of referencing this package.
/// </summary>
public class SelfCertifiedScuInfo
{
    /// <summary>Always "SelfCertified": no certification body, the publisher attests conformity itself.</summary>
    public string CertificationBody { get; set; } = SelfCertifiedFRSSCD.CertificationBody;

    public string Siret { get; set; } = "";

    public string CertificateSerialNumber { get; set; } = "";

    public string SoftwareName { get; set; } = "";

    public string SoftwareVersion { get; set; } = "";

    /// <summary>True once the configured key material was loaded successfully.</summary>
    public bool SignatureCreationDataAvailable { get; set; }

    public FRSSCDInfo ToFRSSCDInfo() => new()
    {
        Description = $"fiskaltrust Middleware SCU FR (self-certified), SIRET {Siret}",
        Version = SoftwareVersion,
        InfoData = JsonSerializer.Serialize(this),
    };
}
