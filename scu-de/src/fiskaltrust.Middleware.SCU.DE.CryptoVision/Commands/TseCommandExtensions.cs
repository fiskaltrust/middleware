namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File
{
    public static class TseCommandExtensions
    {
        public static byte[] ToBytes(this TseCommandCodeEnum commandCode) => ((ushort) commandCode).ToBytes();

        public static byte[] ToBytes(this ushort number) => new byte[] { (byte) ((number >> 8) % 0x100), (byte) (number % 0x100) };
    }
}
