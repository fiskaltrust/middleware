namespace fiskaltrust.Middleware.SCU.IT.CustomRTServer
{
    public class CustomRTServerConfiguration
    {
        public string? ServerUrl { get; set; }
        public double ClientTimeoutMs { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Username { get; set; }= string.Empty;
    }
}