using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.SCU.DE.SwissbitV2.Helpers;
using fiskaltrust.Middleware.SCU.DE.SwissbitV2.Interop;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.SwissbitV2.Extensions
{
    public static class ServiceCollectionExtensions
    {

        public static void AddSwissbitScuServices(this IServiceCollection serviceCollection, Dictionary<string, object> config)
        {
            serviceCollection.AddSingleton<LockingHelper>();
            serviceCollection.AddSingleton(JsonConvert.DeserializeObject<SwissbitV2SCUConfiguration>(JsonConvert.SerializeObject(config)));
            serviceCollection.AddSingleton<INativeFunctionPointerFactory, FunctionPointerFactory>();
            serviceCollection.AddScoped<IDESSCD, SwissbitV2SCU>();
        }
    }
}