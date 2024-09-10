namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2
{
    public class SwissbitCloudV2SCUConfiguration
    {
        public string SerialNumber { get; set; } = "54e1d00a80434c617c850a31dd7346b1";
        public string AccessToken { get; set; } = "7e7da19d34b41c367b4340c02c86a87c8639a70a200f7cc3a39ad8d8c4e3153d";
        public bool EnableTarFileExport { get; set; } = true;
        public virtual string CertificationId { get; set; } = "BSI-K-TR-0490-2021";
        public bool DisplayCertificationIdAddition { get; set; } = false;
        public string CertificationIdAddition { get; set; }
        public string ApiEndpoint { get; set; } = "https://dev.web-tse.de";
        public int SwissbitCloudV2Timeout { get; set; } = 120000;
        public string ProxyServer { get; set; }
        public int? ProxyPort { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public int? MaxClientCount { get; set; }
        public int RetriesOn5xxError { get; set; } = 2;
        public int RetriesOnTarExportWebException{ get; set; } = 2;
        public int DelayOnRetriesInMs { get; set; } = 1000;
    }
}
