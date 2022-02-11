using System;
using System.Collections.Generic;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using fiskaltrust.Middleware.SCU.DE.Epson.Communication;
using fiskaltrust.Middleware.SCU.DE.Epson.Commands;

namespace fiskaltrust.Middleware.SCU.DE.Epson
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {          
            services.AddSingleton(JsonConvert.DeserializeObject<EpsonConfiguration>(JsonConvert.SerializeObject(Configuration)));
            services.AddSingleton<TcpCommunicationQueue>();
            services.AddSingleton<OperationalCommandProvider>();
            services.AddScoped<IDESSCD, EpsonSCU>();
        }
    }
}
