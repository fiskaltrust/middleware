using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.StaticLib;
using fiskaltrust.Middleware.SCU.DE.SwissbitBase.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitAndroid
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<LockingHelper>();
            serviceCollection.AddSingleton(new ConfigurationDictionary(Configuration));
            serviceCollection.AddSingleton<INativeFunctionPointerFactory>(new FunctionPointerFactory());
            serviceCollection.AddScoped<IDESSCD, SwissbitSCU>();
        }
    }
}
