namespace fiskaltrust.Middleware.SCU.DE.SwissbitV2
{
    public class SwissbitV2SCUConfiguration
    {
        public string DevicePath { get; set; }
        public string AdminPin { get; set; } = "12345";
        public string TimeAdminPin { get; set; } = "98765";
        public bool EnableTarFileExport { get; set; } = true;
        public int TooLargeToExportThreshold { get; set; } = 100 * 1024 * 1024;  // 100 MB
        public bool EnableFirmwareUpdate { get; set; } = true;
        public string NativeLibArch { get; set; }
        public bool StoreTemporaryExportFiles { get; set; } = false;
        public string ServiceFolder { get; set; }
    }
}
