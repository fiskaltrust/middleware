using System;
using System.Collections.Generic;
using System.Text;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using fiskaltrust.Middleware.SCU.DE.SwissbitBase.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<LockingHelper>();
            serviceCollection.AddSingleton(new ConfigurationDictionary(Configuration));
            serviceCollection.AddSingleton<INativeFunctionPointerFactory>(new Interop.DynamicLib.FunctionPointerFactory(Configuration.ContainsKey(SwissbitSCU.libraryFileKeyName) ? Configuration[SwissbitSCU.libraryFileKeyName] as string : null));
            serviceCollection.AddScoped<IDESSCD, SwissbitSCU>();
        }
    }
}
