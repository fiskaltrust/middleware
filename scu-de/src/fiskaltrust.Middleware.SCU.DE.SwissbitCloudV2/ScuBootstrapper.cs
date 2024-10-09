using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Helpers;
using fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;


namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var config = JsonConvert.DeserializeObject<SwissbitCloudV2SCUConfiguration>(JsonConvert.SerializeObject(Configuration));
            services.AddSingleton(config);
            services.AddScoped<ISwissbitCloudV2ApiProvider, SwissbitCloudV2ApiProvider>();
            services.AddScoped<ClientCache>();
            services.AddScoped<IDESSCD, SwissbitCloudV2SCU>();
            services.AddScoped<HttpClientWrapper, HttpClientWrapper>();
        }
    }
}
