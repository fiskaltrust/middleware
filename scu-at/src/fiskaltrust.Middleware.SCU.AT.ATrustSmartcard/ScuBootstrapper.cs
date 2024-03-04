using fiskaltrust.ifPOS.v2.at;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Helpers;
using fiskaltrust.Middleware.SCU.AT.ATrustSmartcard.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.AT.ATrustSmartcard
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(JsonConvert.DeserializeObject<ATrustSmartcardSCUConfiguration>(JsonConvert.SerializeObject(Configuration), new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }));
            services.AddScoped<LockHelper>();
            services.AddScoped<CardServiceFactory>();
            services.AddScoped<IATSSCD, ATrustSmartcardSCU>();
        }
    }
}
