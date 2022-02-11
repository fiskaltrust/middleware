using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands
{
    public static class MaintenanceAndTimeSynchronizationCommands
    {
        public static TseCommand CreateUpdateTimeTseCommand(long unixTime) => new TseCommand(TseCommandCodeEnum.UpdateTime, 0x0000, new TseByteArrayParameter(new byte[] { (byte) (unixTime >> 24), (byte) (unixTime >> 16), (byte) (unixTime >> 8), (byte) unixTime }));
    }
}
