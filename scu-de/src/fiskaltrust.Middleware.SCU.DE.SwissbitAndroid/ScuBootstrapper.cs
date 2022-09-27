using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.Swissbit;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitAndroid
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<LockingHelper>();
            serviceCollection.AddSingleton(JsonConvert.DeserializeObject<SwissbitSCUConfiguration>(JsonConvert.SerializeObject(Configuration)));
            serviceCollection.AddSingleton<INativeFunctionPointerFactory, FunctionPointerFactory>();
            serviceCollection.AddScoped<IDESSCD, SwissbitSCU>();
        }
    }
}
