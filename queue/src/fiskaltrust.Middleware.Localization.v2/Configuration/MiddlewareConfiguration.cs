namespace fiskaltrust.Middleware.Localization.v2.Configuration;

public class MiddlewareConfiguration
{
    public Guid QueueId { get; set; }
    public Guid CashBoxId { get; set; }
    public int ReceiptRequestMode { get; set; }
    public bool IsSandbox { get; set; }
    public string? ServiceFolder { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
    public Dictionary<string, bool>? PreviewFeatures { get; set; }
    public string ProcessingVersion { get; set; }
    public string AssemblyName { get; set; }
    public Version AssemblyVersion { get; set; }
    public string? LauncherVersion { get; set; }
    public string? LauncherEnvironment { get; set; }
}
