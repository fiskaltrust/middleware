using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        private const string win32LibraryFile = "runtimes\\win-x86\\native\\WormAPI.dll";
        private const string win64LibraryFile = "runtimes\\win-x64\\native\\WormAPI.dll";
        private const string linux32LibraryFile = "runtimes/linux/native/libWormAPI.so";
        private const string linux64LibraryFile = "runtimes/linux-x64/native/libWormAPI.so";
        private const string linuxArm32LibraryFile = "runtimes/linux-arm/native/libWormAPI.so";

        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            CopyLibraryToWorkingDirectory();

            serviceCollection.AddSingleton<LockingHelper>();
            serviceCollection.AddSingleton(JsonConvert.DeserializeObject<SwissbitSCUConfiguration>(JsonConvert.SerializeObject(Configuration)));
            serviceCollection.AddSingleton<INativeFunctionPointerFactory, FunctionPointerFactory>();
            serviceCollection.AddScoped<IDESSCD, SwissbitSCU>();
        }

        private void CopyLibraryToWorkingDirectory()
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
                        Architecture.Arm => linuxArm32LibraryFile,
                        Architecture.Arm64 => linuxArm32LibraryFile,
                        //Architecture.Arm64 => throw new NotImplementedException("Arm64 is currently not supported by the Swissbit hardware TSE SDK."),
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
            Console.WriteLine(currentDirectory);
            File.Copy(Path.Combine(currentDirectory, libraryFile), Path.Combine(currentDirectory, Path.GetFileName(libraryFile)), true);
        }
    }
}
