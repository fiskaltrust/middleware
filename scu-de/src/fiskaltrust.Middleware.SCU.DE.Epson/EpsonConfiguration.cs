namespace fiskaltrust.Middleware.SCU.DE.Epson
{
    public class EpsonConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; } = 8009;
        public string DeviceId { get; set; } = "local_TSE";
        public int Timeout { get; set; } = 60000;
        public bool EnableTarFileExport { get; set; } = true;
        public string AdminUser { get; set; } = "Administrator";
        public string AdminPin { get; set; } = "11111";
        public string TimeAdminUser { get; set; } = "TimeAdmin";
        public string TimeAdminPin { get; set; } = "22222";
        public string DefaultSharedSecret { get; set; } = "EPSONKEY";
        public string StorageType { get; set; } = "type_storage";
        public string Puk { get; set; } = "123456";
    }
}
