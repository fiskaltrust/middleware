using fiskaltrust.ifPOS.v1.at;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.AT.PrimeSignHSM
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(JsonConvert.DeserializeObject<PrimeSignSCUConfiguration>(JsonConvert.SerializeObject(Configuration)));
            services.AddScoped<IATSSCD, PrimeSignSCU>();
        }
    }
}
