using System;
using System.Collections.Generic;
using System.Net.Http;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTPrinter
{
    public class ScuBootstrapper : IMiddlewareBootstrapper
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = null!;

        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            var epsonScuConfig = JsonConvert.DeserializeObject<EpsonRTPrinterSCUConfiguration>(JsonConvert.SerializeObject(Configuration));

            serviceCollection.AddSingleton(epsonScuConfig);

            serviceCollection.AddHttpClient<IEpsonFpMateClient, LocalEpsonFpMateClient>()
                .ConfigureHttpClient((_, client) =>
                {
                    client.BaseAddress = new Uri(epsonScuConfig.DeviceUrl ?? throw new InvalidOperationException("DeviceUrl is required"));
                    client.Timeout = TimeSpan.FromMilliseconds(epsonScuConfig.ClientTimeoutMs);
                });

            serviceCollection.AddHttpClient<IPdfReceiptClient, PdfReceiptClient>()
                .ConfigureHttpClient((_, client) =>
                {
                    client.Timeout = TimeSpan.FromMilliseconds(epsonScuConfig.ClientTimeoutMs);
                });

            serviceCollection.AddScoped<IITSSCD, EpsonRTPrinterSCU>();
        }
    }
}
