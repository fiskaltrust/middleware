using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter
{
    public class EpsonRTPrinterSCUConfiguration
    {
        /// <summary>
        /// The URL or IP address of the RT Printer or Server, e.g. http://192.168.0.100
        /// </summary>
        public string? DeviceUrl { get; set; }

        /// <summary>
        /// The HTTP client timeout used when communicating with the RT Printer or Server
        /// </summary>
        public int ClientTimeoutMs { get; set; } = 15000;

        /// <summary>
        /// The server/printer timeout for executing commands
        /// </summary>
        public int ServerTimeoutMs { get; set; } = 10000;

        public string? Password { get; set; }

        public string? AdditionalTrailerLines { get; set; }

        /// <summary>
        /// Base URL of the PDF signing service exposing /Fiskaltrust/getPDF.php.
        /// Injected by the host configuration. When unset, PDF signature fetching is disabled.
        /// </summary>
        public string? PdfServerUrl { get; set; }
    }
}
