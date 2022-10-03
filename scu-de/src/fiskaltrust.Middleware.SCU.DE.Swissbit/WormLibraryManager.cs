using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit
{
    public static class WormLibraryManager
    {
        private static readonly string _win32LibraryFile = Path.Combine("runtimes", "win-x86", "native", "WormAPI.dll");
        private static readonly string _win64LibraryFile = Path.Combine("runtimes", "win-x64", "native", "WormAPI.dll");
        private static readonly string _linux32LibraryFile = Path.Combine("runtimes", "linux", "native", "libWormAPI.so");
        private static readonly string _linux64LibraryFile = Path.Combine("runtimes", "linux-x64", "native", "libWormAPI.so");
        private static readonly string _linuxArmV7LibraryFile = Path.Combine("runtimes", "linux-arm-v7", "native", "libWormAPI.so");
        private static readonly string _linuxArmV8LibraryFile = Path.Combine("runtimes", "linux-arm-v8", "native", "libWormAPI.so");

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
                        Architecture.X86 => _linux32LibraryFile,
                        Architecture.X64 => _linux64LibraryFile,
                        Architecture.Arm => _linuxArmV7LibraryFile,
                        Architecture.Arm64 => _linuxArmV8LibraryFile,
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
                        Architecture.X86 => _win32LibraryFile,
                        Architecture.X64 => _win64LibraryFile,
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
