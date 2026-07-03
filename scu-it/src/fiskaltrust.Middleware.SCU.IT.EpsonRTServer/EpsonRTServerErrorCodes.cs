using System.Collections.Generic;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer
{
    /// <summary>
    /// RT Server response codes per "RT Server Security Communication Protocol" ch. 7.1.1 (firmware 6.00/6.01).
    /// </summary>
    public static class EpsonRTServerErrorCodes
    {
        private static readonly Dictionary<int, string> _codes = new()
        {
            [0] = "OK",
            [-1] = "Generic error",
            [-2] = "No data",
            [-3] = "No ram",
            [-4] = "Parser error",
            [-5] = "Incomplete XML file",
            [-6] = "Invalid XML command",
            [-7] = "Internal error",
            [-8] = "Server busy",
            [-20] = "Till not present in the map",
            [-21] = "Blockchain error",
            [-22] = "Hash error",
            [-23] = "Daily amount error",
            [-24] = "Receipt amount error",
            [-25] = "Receipt number error",
            [-26] = "RT Server daily closure required",
            [-27] = "RT Server wait daily closure",
            [-28] = "RT Server not in service",
            [-29] = "Error creating RT Server database folder",
            [-30] = "User ID error",
            [-31] = "File not found",
            [-32] = "Refund or void not possible",
            [-33] = "Refund or void with wrong amount",
            [-34] = "Refund or void on previous EJ (deprecated)",
            [-35] = "Refund or void with wrong date reference",
            [-36] = "Receipt with different date from RT Server",
            [-37] = "Duplicate fiscal receipt (probable retry from till); receipt logged, answer positive",
            [-38] = "ATECO code error / payment not equal to fiscal receipt total",
            [-39] = "Fiscal receipt information error (fiscal information values not compatible with the payment)",
            [-40] = "Deposit (acconto) error (for services)",
            [-41] = "Subtotal discount error with modifiers (modificatori)",
            [-42] = "Deferred lottery code error",
            [-43] = "Attribute out of range",
            [-44] = "Attribute code error",
            [-45] = "Refund or void reference document error (time omitted or zero)",
            [-46] = "Receipt cb error (codice bidimensionale contains incorrect data)",
            [-47] = "Internal code for receipts uploaded via upload.cgi",
            [-48] = "VAT index error (vatID out of range or zero-percentage group that is not a nature)",
            [-49] = "Personal tax code (Codice Fiscale) or business tax code (Partita IVA) erroneous",
            [-50] = "Wrong date (till date does not match RT Server date)",
            [-51] = "Invalid XML tag",
            [-52] = "Till offline (10+ fiscal receipts received with a delay of more than 60 minutes)"
        };

        public static string Describe(int code) => _codes.TryGetValue(code, out var description) ? description : "Unknown RT Server error code";

        /// <summary>
        /// Errors indicating that the LOCAL state (CCDC chain seed, daily amount or document counter) is out of
        /// sync with the RT Server: -21 blockchain, -22 hash, -23 daily amount, -25 receipt number. All of them
        /// are recoverable by requesting a new Token, which carries the authoritative counters.
        /// </summary>
        public static bool IsLocalStateOutOfSync(int code) => code is (-21) or (-22) or (-23) or (-25);
    }
}
