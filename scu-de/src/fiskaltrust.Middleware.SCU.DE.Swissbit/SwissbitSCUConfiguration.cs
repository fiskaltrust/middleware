namespace fiskaltrust.Middleware.SCU.DE.Swissbit
{
    public class SwissbitSCUConfiguration
    {
        public string DevicePath { get; set; }
        public string AdminPin { get; set; } = "12345";
        public string TimeAdminPin { get; set; } = "98765";
        public bool EnableTarFileExport { get; set; } = true;
        public int TooLargeToExportThreshold { get; set; } = 100 * 1024 * 1024;  // 100 MB
        public bool EnableFirmwareUpdate { get; set; } = false;
        public string NativeLibArch { get; set; }
        public long ChunkExportTransactionCount { get; set; } = 0;
        public long ChunkExportIteration { get; set; } = 0;
        public bool StoreTemporaryExportFiles { get; set; } = false;
    }
}
