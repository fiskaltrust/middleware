namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal
{
    public class DeutscheFiskalSCUConfiguration
    {
        public string FccDirectory { get; set; }
        public string FccId { get; set; }
        public string FccSecret { get; set; }
        public int? FccPort { get; set; }
        public string ErsCode { get; set; }
        public string ActivationToken { get; set; }
        public bool EnableTarFileExport { get; set; } = true;
        public string FccDownloadUri { get; set; }
        public bool DontAddFccFirewallException { get; set; }
        public int MaxFccDownloadTimeSec { get; set; } = 60 * 60;
        public int ProcessTimeoutSec { get; set; } = 5 * 60;
        public string FccUri { get; set; }
        public virtual string CertificationId { get; set; } = "BSI-K-TR-0474-2021";
        public bool DisplayCertificationIdAddition { get; set; } = true;
        public string CertificationIdAddition { get; set; } = "USK ausgesetzt";
        public string ServiceFolder { get; set; }
        public string FccVersion { get; set; } = "4.0.5";
        public string ProxyServer { get; set; }
        public int? ProxyPort { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public int? FccHeapMemory { get; set; }
        public int FccRetry { get; set; } = 8;
        public bool Disable { get; set; } = false;

    }
}
