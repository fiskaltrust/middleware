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

        /// <summary>
        /// The maximum number of retries when a network error occurs during receipt printing
        /// </summary>
        public int MaxNetworkRetries { get; set; } = 3;

        public string? Password { get; set; }

        public string? AdditionalTrailerLines { get; set;}

        /// <summary>
        /// Automatically reboots the RT printer after a successful daily closing (Z report).
        /// Opt-in workaround for printers that occasionally get stuck during the day (#549).
        /// Does not affect the manual (zero-receipt) reboot request.
        /// </summary>
        public bool ForceRebootAfterDailyClosing { get; set; } = false;
    }
}