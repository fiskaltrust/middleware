using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueIT
{
    public class QueueITBootstrapper : ILocalizedQueueBootstrapper
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var _ = services
                .AddScoped<IMarketSpecificSignProcessor, SignProcessor>(x => new SignProcessor(x.GetRequiredService<IConfigurationRepository>(), x.GetRequiredService<SignProcessorIT>()))
                .AddScoped<IMarketSpecificJournalProcessor, JournalProcessorIT>()
                .AddScoped<ReceiptCommandProcessorIT>()
                .AddScoped<ProtocolCommandProcessorIT>()
                .AddScoped<LifecyclCommandProcessorIT>()
                .AddScoped<InvoiceCommandProcessorIT>()
                .AddScoped<DailyOperationsCommandProcessorIT>()
                .AddSingleton(sp => QueueITConfiguration.FromMiddlewareConfiguration(sp.GetRequiredService<MiddlewareConfiguration>()))
                .AddSingleton(sp =>
                {
                    var clientFactory = sp.GetRequiredService<IClientFactory<IITSSCD>>();
                    var middlewareConfiguration = sp.GetRequiredService<MiddlewareConfiguration>();
                    var scuIT = JsonConvert.DeserializeObject<List<ftSignaturCreationUnitIT>>(middlewareConfiguration.Configuration["init_ftSignaturCreationUnitIT"].ToString());
                    var uri = GetUriForSignaturCreationUnit(scuIT.FirstOrDefault().Url);
                    var config = new ClientConfiguration
                    {
                        Url = uri.ToString(),
                        UrlType = uri.Scheme
                    };
                    return clientFactory.CreateClient(config);
                });
        }

        private static Uri GetUriForSignaturCreationUnit(string url)
        {

            try
            {
                var urls = JsonConvert.DeserializeObject<string[]>(url);
                var grpcUrl = urls.FirstOrDefault(x => x.StartsWith("grpc://"));
                url = grpcUrl ?? urls.First();
            }
            catch { }

            return new Uri(url);
        }
    }
}
