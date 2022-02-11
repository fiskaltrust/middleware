using fiskaltrust.Middleware.Localization.QueueDE.Constants;
using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueDE.Extensions
{
    public static class SignatureCreationUnitDEExtensions
    {
        public static bool IsSwitchSource(this ftSignaturCreationUnitDE scu) => (scu.Mode & Modes.SCU.Flags.SwitchSource) > 0x0000;
     
        public static bool IsSwitchTarget(this ftSignaturCreationUnitDE scu) => (scu.Mode & Modes.SCU.Flags.SwitchTarget) > 0x0000;
    }
}
