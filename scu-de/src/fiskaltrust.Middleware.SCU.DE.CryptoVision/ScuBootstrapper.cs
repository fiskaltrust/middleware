using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Native;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(JsonConvert.DeserializeObject<CryptoVisionConfiguration>(JsonConvert.SerializeObject(Configuration)));

            serviceCollection.AddScoped(_ => GetFileInfoForCurrentOS());
            serviceCollection.AddScoped<ITseTransportAdapter, MassStorageClassTransportAdapter>();
            serviceCollection.AddScoped<ICryptoVisionProxy, CryptoVisionFileProxy>();
            serviceCollection.AddScoped<IDESSCD, CryptoVisionSCU>();
        }

        private IOsFileIo GetFileInfoForCurrentOS() => Environment.OSVersion.Platform switch
        {
            PlatformID.MacOSX => new MacOsFileIo(),
            PlatformID.Unix => new LinuxFileIo(),
            _ => new WindowsFileIo()
        };
    }
}
