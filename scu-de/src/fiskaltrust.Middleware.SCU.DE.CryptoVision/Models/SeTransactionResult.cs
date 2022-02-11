namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models
{
    public class SeTransactionResult
    {
        public ulong LogUnixTime { get; set; }
        public byte[] SerialNumber { get; set; }
        public uint SignatureCounter { get; set; }
        public byte[] SignatureValue { get; set; }
    }
}
