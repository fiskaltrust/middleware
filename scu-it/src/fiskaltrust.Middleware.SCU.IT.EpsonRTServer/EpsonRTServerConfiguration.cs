namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer
{
    public class EpsonRTServerConfiguration
    {
        /// <summary>
        /// The URL or IP address of the Epson RT Server, e.g. https://192.168.0.100 or https://2.239.218.86:50191.
        /// The SOAP endpoints /cgi-bin/fpserver.cgi and /cgi-bin/fpmate.cgi are appended automatically.
        /// </summary>
        public string? ServerUrl { get; set; }

        /// <summary>
        /// HTTP Basic authentication user for the RT Server (Manager / Administrator or Till user). Default device user is "epson".
        /// </summary>
        public string Username { get; set; } = "epson";

        /// <summary>
        /// HTTP Basic authentication password for the RT Server. Default device password is "epson".
        /// </summary>
        public string Password { get; set; } = "epson";

        /// <summary>
        /// If true fiscal receipts are sent to the RT Server synchronously; otherwise they are cached locally and
        /// transmitted by the background queue (offline resilience).
        /// </summary>
        public bool SendReceiptsSync { get; set; } = true;

        /// <summary>
        /// If true, non-critical RT Server error codes are logged instead of thrown.
        /// </summary>
        public bool IgnoreRTServerErrors { get; set; } = false;

        /// <summary>
        /// How many times the communication queue retries a cached document that the RT Server actively rejects
        /// before parking it in the "failed" subfolder. Network failures do not count towards this limit.
        /// </summary>
        public int MaxDocumentSendRetries { get; set; } = 5;

        /// <summary>
        /// How many times a request is retried when the RT Server answers -8 "Server busy" (a transient
        /// condition, e.g. while a daily closure or Z report is being processed).
        /// </summary>
        public int ServerBusyRetries { get; set; } = 5;

        /// <summary>
        /// Delay between -8 "Server busy" retries.
        /// </summary>
        public int ServerBusyRetryDelayInMs { get; set; } = 2000;

        /// <summary>
        /// If true, the daily-closing receipt also requests a SERVER-level Z report (printZReport) after closing
        /// the till. The server Z is a device-wide operation that transmits the daily takings to the tax
        /// authority and keeps the device busy for a long time; on multi-till installations it should be left
        /// to the RT Server's own schedule, therefore the default is false (till closure only).
        /// </summary>
        public bool PerformServerZReportOnDailyClosing { get; set; }

        /// <summary>
        /// The HTTP client timeout used when communicating with the RT Server.
        /// </summary>
        public int RTServerHttpTimeoutInMs { get; set; } = 15000;

        /// <summary>
        /// The server-side command timeout appended to the fpmate.cgi endpoint.
        /// </summary>
        public int ServerCommandTimeoutInMs { get; set; } = 10000;

        /// <summary>
        /// Allows self-signed certificates on the RT Server (common for on-premise devices).
        /// </summary>
        public bool DisableSSLValidation { get; set; }

        /// <summary>
        /// Base folder used to persist the per-till state cache. Defaults to the personal folder.
        /// </summary>
        public string? ServiceFolder { get; set; }

        /// <summary>
        /// Overrides the folder used by the communication queue to cache pending documents.
        /// </summary>
        public string? CacheDirectory { get; set; }
    }
}
