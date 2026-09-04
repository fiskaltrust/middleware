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

        /// <summary>
        /// How long a command waits for the device when it is reached through a relay instead of a direct
        /// connection. Ignored by the direct client, which uses <see cref="ClientTimeoutMs"/>.
        /// <para>
        /// It has to leave room, inside the timeout of whoever called the SCU, for this wait plus
        /// <see cref="RecoveryVerdictTimeoutMs"/>: past that point the recovery still reaches the right
        /// answer, but there is nobody left to hand it to.
        /// </para>
        /// </summary>
        public int RelayCommandTimeoutMs { get; set; } = 45000;

        /// <summary>
        /// How long the network-error recovery keeps asking the printer what it did before giving up.
        /// <para>
        /// The recovery concludes only from an answer, never from silence, so this is the window the printer
        /// has to become responsive again in. Raising it makes the recovery more patient; lowering it makes
        /// an unresponsive printer fail faster, always as "state unknown", never as a reprint.
        /// </para>
        /// </summary>
        public int RecoveryVerdictTimeoutMs { get; set; } = 40000;

        /// <summary>
        /// Pause between two status queries while waiting for the printer to become responsive again.
        /// </summary>
        public int RecoveryVerdictPollIntervalMs { get; set; } = 3000;

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