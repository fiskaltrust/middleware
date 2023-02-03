using System;
using System.Collections.Generic;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var config = JsonConvert.DeserializeObject<SwissbitSCUConfiguration>(JsonConvert.SerializeObject(Configuration));
            WormLibraryManager.CopyLibraryToWorkingDirectory(config);

            serviceCollection.AddSwissbitScuServices(Configuration);
        }
    }
}
