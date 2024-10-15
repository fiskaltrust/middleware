namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard
{
    public class ATrustSmartcardSCUConfiguration
    {
        public bool VerifySignature { get; set; }
        public bool ApduSelect { get; set; }
        public bool HealthCheck { get; set; }
        public bool Shared { get; set; }
        public int ReaderTimeoutMs { get; set; } = 60 * 1000;
        public int WatchdogTimeoutMs { get; set; } = 30 * 1000;
        public int Reader { get; set; } = -1;
        public string SerialNumber { get; set; }
    }
}
