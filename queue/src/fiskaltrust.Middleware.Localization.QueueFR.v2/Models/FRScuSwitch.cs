using System.Text.Json.Serialization;
using fiskaltrust.ifPOS.v2.fr;

namespace fiskaltrust.Middleware.Localization.QueueFR.v2.Models;

/// <summary>
/// What tells two SCUs apart for a switch: the body that certified them and the serial of the
/// certificate they sign with. ADR 0005 splits the archive by certificate serial, so a switch that
/// changes neither would leave nothing to split by.
/// </summary>
public class FRScuIdentity
{
    [JsonPropertyName("CertificationBody")]
    public string? CertificationBody { get; set; }

    [JsonPropertyName("CertificateSerialNumber")]
    public string? CertificateSerialNumber { get; set; }

    public static FRScuIdentity From(FRSSCDInfo? info) => new()
    {
        CertificationBody = FRSSCDInfoHelper.GetCertificationBody(info),
        CertificateSerialNumber = FRSSCDInfoHelper.GetCertificateSerialNumber(info),
    };

    public bool SameAs(FRScuIdentity other)
        => string.Equals(CertificationBody, other.CertificationBody, StringComparison.Ordinal)
        && string.Equals(CertificateSerialNumber, other.CertificateSerialNumber, StringComparison.Ordinal);

    public override string ToString() => $"{CertificationBody ?? "unknown body"} / certificate {CertificateSerialNumber ?? "unknown"}";
}

/// <summary>
/// The SCU switch recorded on the <c>FR</c> block of the 0x4011 and 0x4012 responses. The queue
/// rebuilds whether a switch is pending from these receipts, like the rest of its fiscal state.
/// </summary>
public class FRScuSwitch
{
    /// <summary>The SCU that signed the 0x4011 receipt and is being replaced.</summary>
    [JsonPropertyName("Outgoing")]
    public FRScuIdentity Outgoing { get; set; } = new();

    /// <summary>The SCU that signed the 0x4012 receipt; null on the 0x4011 receipt.</summary>
    [JsonPropertyName("Incoming")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FRScuIdentity? Incoming { get; set; }
}
