using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit
{
    public static class WormLibraryManager
    {
        private const string LINUX_LIB = "libWormAPI.so";
        private const string WINDOWS_LIB = "WormAPI.dll";
        
        private const string PATH_RUNTIMES = "runtimes";
        private const string PATH_NATIVE = "native";

        private static readonly string _win32LibraryFile = Path.Combine(PATH_RUNTIMES, "win-x86", PATH_NATIVE, WINDOWS_LIB);
        private static readonly string _win64LibraryFile = Path.Combine(PATH_RUNTIMES, "win-x64", PATH_NATIVE, WINDOWS_LIB);
        private static readonly string _linux32LibraryFile = Path.Combine(PATH_RUNTIMES, "linux-x86", PATH_NATIVE, LINUX_LIB);
        private static readonly string _linux64LibraryFile = Path.Combine(PATH_RUNTIMES, "linux-x64", PATH_NATIVE, LINUX_LIB);
        private static readonly string _linuxArmLibraryFile = Path.Combine(PATH_RUNTIMES, "linux-arm", PATH_NATIVE, LINUX_LIB);
        private static readonly string _linuxArm64LibraryFile = Path.Combine(PATH_RUNTIMES, "linux-arm64", PATH_NATIVE, LINUX_LIB);

        public static void CopyLibraryToWorkingDirectory(SwissbitSCUConfiguration configuration)
        {
            var libraryFile = !string.IsNullOrEmpty(configuration.NativeLibArch)
                ? Path.Combine(PATH_RUNTIMES, configuration.NativeLibArch, PATH_NATIVE, LINUX_LIB)
                : SelectPathBasedOnArchitecture();

            var currentDirectory = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SwissbitSCU)).Location);
            File.Copy(Path.Combine(currentDirectory, libraryFile), Path.Combine(currentDirectory, Path.GetFileName(libraryFile)), true);
        }

        private static string SelectPathBasedOnArchitecture()
        {
            var arch = RuntimeInformation.ProcessArchitecture;

            return Environment.OSVersion.Platform switch
            {
                PlatformID.MacOSX or PlatformID.Unix => arch switch
                {
                    Architecture.X86 => _linux32LibraryFile,
                    Architecture.X64 => _linux64LibraryFile,
                    Architecture.Arm => _linuxArmLibraryFile,
                    Architecture.Arm64 => _linuxArm64LibraryFile,
                    _ => throw new NotImplementedException($"The CPU architecture {arch} is not supported on Unix by the Swissbit hardware TSE SDK.")
                }
                ,
                PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.WinCE or PlatformID.Xbox or _ => arch switch
                {
                    Architecture.X86 => _win32LibraryFile,
                    Architecture.X64 => _win64LibraryFile,
                    _ => throw new NotImplementedException($"The CPU architecture {arch} is currently not supported on Windows by the Swissbit hardware TSE SDK.")
                }
            };
        }
    }
}
