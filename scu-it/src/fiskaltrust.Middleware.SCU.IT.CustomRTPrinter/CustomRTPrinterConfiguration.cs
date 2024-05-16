namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter
{
    public class CustomRTPrinterConfiguration
    {
        public string? DeviceUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int ClientTimeoutMs { get; set; } = 15000;
        public int ServerTimeoutMs { get; set; } = 10000;
    }
}