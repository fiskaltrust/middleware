using fiskaltrust.Middleware.SCU.DE.CryptoVision.Helpers;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision
{
    public class CryptoVisionConfiguration
    {
        public string DevicePath { get; set; }
        public string AdminPin { get; set; }
        public int TseIOTimeout { get; set; } = Constants.DEFAULT_TSE_IO_TIMEOUT;
        public int TseIOReadDelayMs { get; set; } = Constants.DEFAULT_TSE_IO_READ_DELAY_MS;
        public string TimeAdminPin { get; set; }
        public bool RetryOnEmptyResponse { get; set; } = false;
        public bool EnableTarFileExport { get; set; } = true;
        public int? KeepAliveIntervalInSeconds { get; set; }
    }
}
