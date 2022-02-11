namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf
{
    public class DieboldNixdorfConfiguration
    {
        public string ComPort { get; set; }
        public string Url { get; set; }
        public string AdminUser { get; set; }
        public string AdminPin { get; set; }
        public string TimeAdminUser { get; set; }
        public string TimeAdminPin { get; set; }
        public int SlotNumber { get; set; }
        public int ReadTimeoutMs { get; set; } = 1500;
        public int WriteTimeoutMs { get; set; } = 1500;
        public bool EnableDtr { get; set; } = true;
        public bool EnableTarFileExport { get; set; } = true;
        public string ServiceFolder { get; set; }
        public string SscdId { get; set; }
    }
}
