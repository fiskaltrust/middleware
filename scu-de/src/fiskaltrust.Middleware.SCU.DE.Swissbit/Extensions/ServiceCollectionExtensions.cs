using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Helpers;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Extensions
{
    public static class ServiceCollectionExtensions
    {

        public static void AddSwissbitScuServices(this IServiceCollection serviceCollection, Dictionary<string, object> config)
        {
            serviceCollection.AddSingleton<LockingHelper>();
            serviceCollection.AddSingleton(JsonConvert.DeserializeObject<SwissbitSCUConfiguration>(JsonConvert.SerializeObject(config)));
            serviceCollection.AddSingleton<INativeFunctionPointerFactory, FunctionPointerFactory>();
            serviceCollection.AddScoped<IDESSCD, SwissbitSCU>();
        }
    }
}