namespace fiskaltrust.Middleware.SCU.AT.PrimeSignHSM
{
    public class PrimeSignSCUConfiguration
    {
        public bool VerifySignature  { get; set; }
        public string Url { get; set; } = "https://ft-at11-primesignhsm-sandbox.fiskaltrust.at";
        public string SharedSecret { get; set; } = string.Empty;
        public bool SslValidation { get; set; } = true;
    }
}
