namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer.Models
{
    /// <summary>
    /// Parsed representation of the Token returned by the RT Server (createToken / addInfo &lt;token&gt;).
    ///
    /// Layout confirmed against firmware 6.01 (device 99SEA004010), matching the "RT Server Security
    /// Communication Protocol" ch. 6.1. Example: 99SEA004010 AAAA0002 51363 20260702 0743 0001 000000000
    ///
    ///   [0..11)  RT Server fiscal serial number (11)
    ///   [11..19) Till ID (8)
    ///   [19..24) Random number (5)
    ///   [24..32) Current RT Server date, YYYYMMDD (8)
    ///   [32..36) Z Report number (4)
    ///   [36..40) Next expected document number (4)
    ///   [40..49) Daily amount, last two digits are decimals (9)
    /// </summary>
    public class EpsonToken
    {
        public const int ExpectedLength = 49;

        public string Raw { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string TillId { get; set; } = string.Empty;
        public long ZRepNumber { get; set; }

        /// <summary>The next expected document number (first of the day is 0001).</summary>
        public long NextDocNumber { get; set; }

        /// <summary>Daily amount in cents.</summary>
        public long DailyAmountCents { get; set; }

        public static EpsonToken? TryParse(string? token)
        {
            if (string.IsNullOrEmpty(token) || token!.Length < ExpectedLength)
            {
                return null;
            }

            long ParseSegment(int start, int length) => long.TryParse(token.Substring(start, length), out var value) ? value : 0;

            return new EpsonToken
            {
                Raw = token,
                SerialNumber = token.Substring(0, 11),
                TillId = token.Substring(11, 8),
                ZRepNumber = ParseSegment(32, 4),
                NextDocNumber = ParseSegment(36, 4),
                DailyAmountCents = ParseSegment(40, 9)
            };
        }
    }
}
