using fiskaltrust.storage.V0;

namespace fiskaltrust.Middleware.Localization.QueueAT.Extensions
{
    public static class ftSignaturCreationUnitATExtensions
    {
        public static bool IsBackup(this ftSignaturCreationUnitAT scu) => (scu.Mode & 0xff) == 1;
    }
}
