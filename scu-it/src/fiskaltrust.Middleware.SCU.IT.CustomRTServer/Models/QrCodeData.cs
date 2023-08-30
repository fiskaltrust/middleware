namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer;

public class QrCodeData
{
    public string shaMetadata { get; set; } = string.Empty;
    public string signature { get; set; } = string.Empty;
    public string? addInfo { get; set; } = null;
}
