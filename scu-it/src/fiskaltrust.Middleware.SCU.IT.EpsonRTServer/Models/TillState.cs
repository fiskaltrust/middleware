namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models
{
    /// <summary>
    /// Locally persisted state of the blockchain (Token / CCDC chain) and counters for a single till.
    /// This is the Epson RT Server counterpart of the Custom RT Server's QueueIdentification. Instead of an
    /// HMAC key, the security material kept locally is the last fingerprint (CCDC) / Token used to seed the
    /// SHA-256 blockchain of the next document.
    /// </summary>
    public class TillState
    {
        /// <summary>The 8-character Till ID (Store ID + Store Till ID), taken from ftCashBoxIdentification.</summary>
        public string TillId { get; set; } = string.Empty;

        /// <summary>RT Server fiscal serial number (e.g. 99SEA123456), read from serverInfo.</summary>
        public string RTServerSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Section A for the next document: the Token (right after a createToken) or the CCDC/fingerprint of the
        /// last successfully processed document (blockchain link).
        /// </summary>
        public string LastFingerPrint { get; set; } = string.Empty;

        /// <summary>Current Z Report / session number for the till.</summary>
        public long LastZNumber { get; set; }

        /// <summary>Last fiscal receipt number issued for the current session.</summary>
        public long LastDocNumber { get; set; }

        /// <summary>Running daily amount in cents for the till.</summary>
        public long CurrentDailyAmount { get; set; }

        /// <summary>UTC offset reported by the server (1 = winter, 2 = summer). Used in the instant lottery cb string.</summary>
        public int SrtUtcOffset { get; set; } = 1;

        /// <summary>True once a Token has been requested and the blockchain is initialised for the current session.</summary>
        public bool TokenInitialized { get; set; }
    }
}
