﻿using System;
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

            services.AddSingleton(sp =>
            {
                CreateConfigurationActionJournalAsync(middlewareConfiguration, sp.GetRequiredService<IActionJournalRepository>()).Wait();
                return middlewareConfiguration;
            });

            services.AddScoped<ICryptoHelper, CryptoHelper>();
            services.AddScoped<SignProcessor>();
            services.AddScoped<ISignProcessor>(x => new LocalQueueSynchronizationDecorator(x.GetRequiredService<SignProcessor>(), x.GetRequiredService<ILogger<LocalQueueSynchronizationDecorator>>()));
            services.AddScoped<IJournalProcessor, JournalProcessor>();
            services.AddScoped<IPOS, Queue>();
            var businessLogicFactoryBoostrapper = LocalizedQueueBootStrapperFactory.GetBootstrapperForLocalizedQueue(_activeQueueId, middlewareConfiguration);
            businessLogicFactoryBoostrapper.ConfigureServices(services);
        }

        private static async Task CreateConfigurationActionJournalAsync(MiddlewareConfiguration middlewareConfiguration, IActionJournalRepository actionJournalRepository)
        {
            var configurationCopy = new Dictionary<string, object>(middlewareConfiguration.Configuration)
            {
                { "MachineName", Environment.MachineName },
                { "ProcessArchitecture", RuntimeInformation.ProcessArchitecture.ToString() },
                { "OSArchitecture", RuntimeInformation.OSArchitecture.ToString() },
                { "OSDescription", RuntimeInformation.OSDescription.ToString() }
            };
            configurationCopy.Remove("configuration");
            foreach (var key in configurationCopy.Keys.ToArray())
            {
                if (key.ToLowerInvariant().Contains("connectionstring") || key.ToLowerInvariant().EndsWith("_encrypted"))
                {
                    configurationCopy[key] = "********";
                }
            }

            var actionJournal = new ftActionJournal
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = middlewareConfiguration.QueueId,
                ftQueueItemId = Guid.Empty,
                Moment = DateTime.UtcNow,
                Priority = 0,
                Type = nameof(MiddlewareConfiguration),
                Message = "Queue started",
                DataJson = JsonConvert.SerializeObject(new MiddlewareConfiguration
                {
                    CashBoxId = middlewareConfiguration.CashBoxId,
                    QueueId = middlewareConfiguration.QueueId,
                    IsSandbox = middlewareConfiguration.IsSandbox,
                    ServiceFolder = middlewareConfiguration.ServiceFolder,
                    Configuration = configurationCopy,
                    PreviewFeatures = middlewareConfiguration.PreviewFeatures
                }),
                TimeStamp = DateTime.UtcNow.Ticks
            };

            await actionJournalRepository.InsertAsync(actionJournal);
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
