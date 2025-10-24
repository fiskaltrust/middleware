namespace fiskaltrust.Middleware.SCU.BE.ZwarteDoos;

public class ZwarteDoosApiClientConfiguration
{
    public string DeviceId { get; set; } = null!;
    public string SharedSecret { get; set; } = null!;
    public string BaseUrl { get; set; } = "https://sdk.zwartedoos.be";
    public int TimeoutSeconds { get; set; } = 30;
}
