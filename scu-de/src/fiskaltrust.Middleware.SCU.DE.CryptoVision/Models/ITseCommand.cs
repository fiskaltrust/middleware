namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File
{
    public interface ITseCommand
    {
        byte[] ResponseModeBytes { get; }
        byte[] GetCommandDataBytes();
    }
}
