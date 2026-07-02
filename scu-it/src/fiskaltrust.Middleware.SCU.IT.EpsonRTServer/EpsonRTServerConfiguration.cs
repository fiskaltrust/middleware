using System;

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
        /// Optional JSON with the account master data (AccountId, VatId, TaxId, ...), mirroring the Custom RT Server SCU.
        /// </summary>
        public string AccountMasterData { get; set; } = string.Empty;

        /// <summary>
        /// If true the till map (createTills) is programmed automatically during the initial-operation receipt.
        /// The default device till map has to contain the till id otherwise.
        /// </summary>
        public bool AutoProgramTillMap { get; set; } = true;

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

    public class AccountMasterData
    {
        public Guid AccountId { get; set; }

        public string? AccountName { get; set; }

        public string? Street { get; set; }

        public string? Zip { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public string? TaxId { get; set; }

        public string? VatId { get; set; }
    }
}
