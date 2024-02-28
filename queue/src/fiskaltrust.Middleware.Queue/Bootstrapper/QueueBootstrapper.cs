using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using Org.BouncyCastle.Utilities;

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

        public void ConfigureServices(IServiceCollection services)
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
            services.AddSingleton(sp => SaveConfiguration(middlewareConfiguration, sp.GetRequiredService<IActionJournalRepository>()).Result);
            services.AddScoped<ICryptoHelper, CryptoHelper>();
            services.AddScoped<SignProcessor>();
            services.AddScoped<ISignProcessor>(x => new LocalQueueSynchronizationDecorator(x.GetRequiredService<SignProcessor>(), x.GetRequiredService<ILogger<LocalQueueSynchronizationDecorator>>()));
            services.AddScoped<IJournalProcessor, JournalProcessor>();
            services.AddScoped<IPOS, Queue>();
            var businessLogicFactoryBoostrapper = LocalizedQueueBootStrapperFactory.GetBootstrapperForLocalizedQueue(_activeQueueId, middlewareConfiguration);
            businessLogicFactoryBoostrapper.ConfigureServices(services);

        }

        private static async Task<MiddlewareConfiguration> SaveConfiguration(MiddlewareConfiguration middlewareConfiguration, IActionJournalRepository actionJournalRepository)
        {
            middlewareConfiguration.Configuration.Add("MachineName", Environment.MachineName);
            middlewareConfiguration.Configuration.Add("ProcessArchitecture", RuntimeInformation.ProcessArchitecture.ToString());
            middlewareConfiguration.Configuration.Add("OSArchitecture", RuntimeInformation.OSArchitecture.ToString());
            middlewareConfiguration.Configuration.Add("OSDescription", RuntimeInformation.OSDescription.ToString());

            var actionJournal = new ftActionJournal()
            {
             ftActionJournalId = Guid.NewGuid(),
                ftQueueId = middlewareConfiguration.QueueId,
                ftQueueItemId = Guid.NewGuid(),
                Moment = DateTime.Now,
                Priority = 0,
                Type = "Configuration",
                Message = "Configuration",
                DataBase64 = "",
                DataJson = JsonConvert.SerializeObject(middlewareConfiguration),
                TimeStamp = DateTime.Now.Ticks
            };
            await actionJournalRepository.InsertAsync(actionJournal);
            return middlewareConfiguration;

        }

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
