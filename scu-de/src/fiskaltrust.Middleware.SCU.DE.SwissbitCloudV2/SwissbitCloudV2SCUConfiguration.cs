namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2
{
    public class SwissbitCloudV2SCUConfiguration
    {
        public string TseSerialNumber { get; set; }
        public string TseAccessToken { get; set; }
        public bool EnableTarFileExport { get; set; } = true;
        public virtual string CertificationId { get; set; } = "TBD";
        public bool DisplayCertificationIdAddition { get; set; } = false;
        public string CertificationIdAddition { get; set; }
        public string ApiEndpoint {
            get
            {
                if (Sandbox)
                {
                    return "https://dev.web-tse.de";
                }
                else
                {
                    return "https://web-tse.de";
                }
            } 
        } 
        public int SwissbitCloudV2Timeout { get; set; } = 120000;
        public string ProxyServer { get; set; }
        public int? ProxyPort { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public int? MaxClientCount { get; set; }
        public int RetriesOn5xxError { get; set; } = 2;
        public int RetriesOnTarExportWebException{ get; set; } = 2;
        public int DelayOnRetriesInMs { get; set; } = 1000;
        public int ExportTimeoutMs { get; set; } = 18000 * 1000;
        public bool Sandbox { get; set; }
    }
}
