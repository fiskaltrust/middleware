using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Extensions;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Queue.Helpers;
using fiskaltrust.Middleware.QueueSynchronizer;
using fiskaltrust.storage.V0;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Queue.Bootstrapper
{
    public class QueueBootstrapper
    {
        private readonly Dictionary<string, object> _configuration;
        private readonly Guid _activeQueueId;

        public QueueBootstrapper(Guid queueId, Dictionary<string, object> configuration)
        {
            _configuration = configuration;
            _activeQueueId = queueId;
        }

        public ILocalizedQueueBootstrapper ConfigureServicesInner(IServiceCollection services)
        {
            var middlewareConfiguration = new MiddlewareConfiguration
            {
                CashBoxId = GetQueueCashbox(_activeQueueId, _configuration),
                QueueId = _activeQueueId,
                IsSandbox = _configuration.TryGetValue("sandbox", out var sandbox) && bool.TryParse(sandbox.ToString(), out var sandboxBool) && sandboxBool,
                ServiceFolder = _configuration.TryGetValue("servicefolder", out var val) ? val.ToString() : GetServiceFolder(),
                Configuration = _configuration,
                PreviewFeatures = GetPreviewFeatures(_configuration)
            };

            services.AddSingleton(middlewareConfiguration);
            services.AddScoped<ICryptoHelper, CryptoHelper>();
            services.AddScoped<SignProcessor>();
            services.AddScoped<ISignProcessor>(x => new LocalQueueSynchronizationDecorator(x.GetRequiredService<SignProcessor>(), x.GetRequiredService<ILogger<LocalQueueSynchronizationDecorator>>()));
            services.AddScoped<IJournalProcessor, JournalProcessor>();
            services.AddScoped<IPOS, Queue>();

            return LocalizedQueueBootStrapperFactory.GetBootstrapperForLocalizedQueue(_activeQueueId, middlewareConfiguration);
        }
        public void ConfigureServices(IServiceCollection services) => ConfigureServicesInner(services).ConfigureServices(services);

        public async Task<Func<IServiceProvider, Task>> ConfigureServicesAsync(IServiceCollection services) => await ConfigureServicesInner(services).ConfigureServicesAsync(services);

        private static Guid GetQueueCashbox(Guid queueId, Dictionary<string, object> configuration)
        {
            var key = "init_ftQueue";
            if (configuration.ContainsKey(key))
            {
                var queues = JsonConvert.DeserializeObject<List<ftQueue>>(configuration[key].ToString());
                return queues.Where(q => q.ftQueueId == queueId).FirstOrDefault().ftCashBoxId;
            }
            else
            {
                throw new ArgumentException("Configuration must contain 'init_ftQueue' parameter.");
            }
        }

        private static Dictionary<string, bool> GetPreviewFeatures(Dictionary<string, object> configuration)
        {
            var key = "previewFeatures";
            try
            {
                return configuration.ContainsKey(key)
                ? JsonConvert.DeserializeObject<Dictionary<string, bool>>(configuration[key].ToString())
                : new Dictionary<string, bool>();
            }
            catch
            {
                return new();
            }
        }

        private static string GetServiceFolder() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "fiskaltrust", "service");
    }
}
