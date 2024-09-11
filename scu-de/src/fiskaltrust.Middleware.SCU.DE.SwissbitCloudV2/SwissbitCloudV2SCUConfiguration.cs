namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2
{
    public class SwissbitCloudV2SCUConfiguration
    {
        public string SerialNumber { get; set; } = "fd79e44187bce2e2dcc886c89bf993df26d157503c4d953557b2e5af73571876";
        public string AccessToken { get; set; } = "6945c6ab69f348cd3779b5ee139466c4";
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
