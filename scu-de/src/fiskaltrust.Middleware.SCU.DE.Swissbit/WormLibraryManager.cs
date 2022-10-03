using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit
{
    public static class WormLibraryManager
    {
        private const string win32LibraryFile = "runtimes\\win-x86\\native\\WormAPI.dll";
        private const string win64LibraryFile = "runtimes\\win-x64\\native\\WormAPI.dll";
        private const string linux32LibraryFile = "runtimes/linux/native/libWormAPI.so";
        private const string linux64LibraryFile = "runtimes/linux-x64/native/libWormAPI.so";
        private const string linuxArmV7LibraryFile = "runtimes/linux-arm-v7/native/libWormAPI.so";
        private const string linuxArmV8LibraryFile = "runtimes/linux-arm-v8/native/libWormAPI.so";

        public static void CopyLibraryToWorkingDirectory()
        {
            var arch = RuntimeInformation.ProcessArchitecture;

            string libraryFile;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                {
                    libraryFile = arch switch
                    {
                        Architecture.X86 => linux32LibraryFile,
                        Architecture.X64 => linux64LibraryFile,
                        Architecture.Arm => linuxArmV7LibraryFile,
                        Architecture.Arm64 => linuxArmV8LibraryFile,
                        _ => throw new NotImplementedException($"The CPU architecture {arch} is not supported on Unix by the Swissbit hardware TSE SDK.")
                    };
                };
                break;
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                case PlatformID.Xbox:
                default:
                {
                    libraryFile = arch switch
                    {
                        Architecture.X86 => win32LibraryFile,
                        Architecture.X64 => win64LibraryFile,
                        _ => throw new NotImplementedException($"The CPU architecture {arch} is currently not supported on Windows by the Swissbit hardware TSE SDK.")
                    };
                };
                break;
            }

            var currentDirectory = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SwissbitSCU)).Location);
            File.Copy(Path.Combine(currentDirectory, libraryFile), Path.Combine(currentDirectory, Path.GetFileName(libraryFile)), true);
        }
    }
}
