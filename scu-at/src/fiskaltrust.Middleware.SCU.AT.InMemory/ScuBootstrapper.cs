using fiskaltrust.ifPOS.v2.at;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace fiskaltrust.Middleware.SCU.AT.InMemory
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IATSSCD, InMemorySCU>();
        }
    }
}
