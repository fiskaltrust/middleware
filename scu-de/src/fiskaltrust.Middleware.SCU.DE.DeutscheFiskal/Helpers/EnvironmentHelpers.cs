using System;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers
{
    public static class EnvironmentHelpers
    {
        // Check for 128 because of backwards compatibility
        public static bool IsLinux => Environment.OSVersion.Platform == PlatformID.Unix || (int) Environment.OSVersion.Platform == 128;

        public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;
    }
}
